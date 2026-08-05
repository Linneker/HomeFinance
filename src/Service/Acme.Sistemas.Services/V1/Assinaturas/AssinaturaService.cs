using Acme.Sistemas.Domain.Entities.Assinaturas;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.ExternalIntegration.Pagamentos;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Assinaturas;

/// <inheritdoc cref="IAssinaturaService"/>
public sealed class AssinaturaService(
    IAssinaturaWriteRepository assinaturas,
    IPlanoReadRepository planos,
    IEnumerable<IProvedorDePagamento> provedores,
    IContextoRequisicao contexto,
    TimeProvider relogio,
    ILogger<AssinaturaService> logger)
    : IAssinaturaService
{
    /// <summary>
    /// Tolerância depois do vencimento antes de suspender.
    /// </summary>
    /// <remarks>
    /// Sete dias cobrem o cartão que falhou numa tentativa e passou na seguinte, que é o caso
    /// comum. Suspender no dia seguinte ao vencimento tiraria os recursos de quem não deixou de
    /// pagar — e recuperar cliente irritado custa mais do que uma semana de tolerância.
    /// </remarks>
    public static readonly TimeSpan Carencia = TimeSpan.FromDays(7);

    public const string CodigoPlanoInexistente = "plano_inexistente";
    public const string CodigoJaAssinante = "assinatura_ja_vigente";
    public const string CodigoComprovanteNaoConfirmado = "comprovante_nao_confirmado";
    public const string CodigoComprovanteEmOutraConta = "comprovante_em_outra_conta";
    public const string CodigoCanalIndisponivel = "canal_indisponivel";
    public const string CodigoSemUsuario = "usuario_obrigatorio";

    public async Task<SituacaoDaConta> SituacaoAtualAsync(CancellationToken cancellationToken)
    {
        if (contexto.UsuarioId is null)
        {
            return SituacaoDaConta.SemAssinatura;
        }

        var assinatura = await assinaturas.VigenteDoUsuarioAsync(
            contexto.UsuarioId.Value, cancellationToken);

        if (assinatura is null)
        {
            return SituacaoDaConta.SemAssinatura;
        }

        var situacao = assinatura.SituacaoEm(relogio.GetUtcNow(), Carencia);
        var plano = await planos.ObterPorIdAsync(assinatura.PlanoId, cancellationToken);

        // A cota usada só importa para o dono decidir se pode conceder mais um. Em tenant de
        // terceiro a pergunta nem faz sentido, e a consulta contaria os compartilhamentos de
        // quem não é ele.
        var usada = contexto.TenantId is not null && !contexto.SomenteLeitura
            ? await assinaturas.ContarCompartilhamentosVigentesAsync(cancellationToken)
            : 0;

        return new SituacaoDaConta(
            situacao, plano?.Codigo ?? string.Empty, plano?.CotaCompartilhamentos ?? 0, usada);
    }

    public async Task<bool> DonoDoTenantVigenteAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var assinatura = await assinaturas.DoDonoDoTenantAsync(tenantId, cancellationToken);

        return assinatura is not null
               && assinatura.SituacaoEm(relogio.GetUtcNow(), Carencia)
                   is SituacaoAssinatura.Ativa or SituacaoAssinatura.Carencia;
    }

    public async Task<ResultadoDaContratacao> ContratarAsync(
        string planoCodigo,
        CanalPagamento canal,
        string comprovante,
        CancellationToken cancellationToken)
    {
        if (contexto.UsuarioId is null)
        {
            return ResultadoDaContratacao.Recusada(CodigoSemUsuario, "Titular não identificado.");
        }

        var plano = await planos.ObterPorCodigoAsync(planoCodigo, cancellationToken);

        if (plano is null || !plano.Ativo)
        {
            return ResultadoDaContratacao.Recusada(
                CodigoPlanoInexistente, "Plano não disponível para contratação.");
        }

        // Uma assinatura viva por conta. Deixar contratar a segunda cobraria duas vezes por um
        // produto que só entrega uma — e o índice único recusaria depois, com erro de banco.
        var atual = await assinaturas.VigenteDoUsuarioAsync(contexto.UsuarioId.Value, cancellationToken);

        if (atual is not null && atual.SituacaoEm(relogio.GetUtcNow(), Carencia) != SituacaoAssinatura.Suspensa)
        {
            return ResultadoDaContratacao.Recusada(
                CodigoJaAssinante, "Esta conta já tem uma assinatura vigente.");
        }

        var provedor = provedores.FirstOrDefault(p => p.Canal == canal);

        if (provedor is null)
        {
            return ResultadoDaContratacao.Recusada(
                CodigoCanalIndisponivel, "Canal de pagamento indisponível.");
        }

        var confirmacao = await provedor.ConfirmarAsync(comprovante, cancellationToken);

        if (!confirmacao.Confirmado)
        {
            // A tentativa é registrada em log e nenhum direito é concedido. Indisponibilidade do
            // provedor cai aqui também: manter o estado anterior é seguro dos dois lados.
            logger.LogWarning(
                "Contratação recusada no canal {Canal}: {Motivo}.", canal, confirmacao.Motivo);

            return ResultadoDaContratacao.Recusada(
                confirmacao.Motivo == "provedor_indisponivel"
                    ? CodigoCanalIndisponivel
                    : CodigoComprovanteNaoConfirmado,
                "O provedor não confirmou esta compra.");
        }

        var referencia = confirmacao.Referencia!;

        // O mesmo comprovante não ativa duas contas do aplicativo. É a regra que impede comprar
        // uma vez e liberar a família inteira — e ela vale mesmo quando a outra conta é do mesmo
        // titular, porque a assinatura é da conta, não da pessoa na loja.
        var jaUsado = await assinaturas.PorReferenciaAsync(canal, referencia, cancellationToken);

        if (jaUsado is not null && jaUsado.UsuarioId != contexto.UsuarioId.Value)
        {
            return ResultadoDaContratacao.Recusada(
                CodigoComprovanteEmOutraConta, "Esta compra já está vinculada a outra conta.");
        }

        var agora = relogio.GetUtcNow();

        // Suspensa que volta a pagar reaproveita a linha: manter o histórico junto é o que
        // permite ao suporte explicar a sequência de eventos daquela conta.
        if (atual is not null)
        {
            atual.Situacao = SituacaoAssinatura.Ativa;
            atual.PeriodoFim = confirmacao.PeriodoFim!.Value;
            atual.CarenciaAte = null;
            atual.AtualizadoEm = agora;

            await assinaturas.AtualizarSituacaoAsync(atual, cancellationToken);

            return ResultadoDaContratacao.Contratada(atual.Id);
        }

        var nova = new AssinaturaEntity
        {
            Id = Guid.CreateVersion7(),
            UsuarioId = contexto.UsuarioId.Value,
            PlanoId = plano.Id,
            Situacao = SituacaoAssinatura.Ativa,
            Canal = canal,
            PeriodoFim = confirmacao.PeriodoFim!.Value,
            ComprovanteReferencia = referencia,
            CriadoEm = agora,
        };

        await assinaturas.CriarAsync(nova, cancellationToken);

        return ResultadoDaContratacao.Contratada(nova.Id);
    }

    public async Task<ResultadoDaNotificacao> AplicarNotificacaoAsync(
        CanalPagamento canal,
        string identificador,
        EventoDoProvedor evento,
        string referencia,
        string corpo,
        CancellationToken cancellationToken)
    {
        var agora = relogio.GetUtcNow();

        var inedita = await assinaturas.RegistrarNotificacaoAsync(
            canal, identificador, corpo, agora, cancellationToken);

        if (!inedita)
        {
            return ResultadoDaNotificacao.Repetida;
        }

        var assinatura = await assinaturas.PorReferenciaAsync(canal, referencia, cancellationToken);

        if (assinatura is null)
        {
            // Notificação de assinatura que não é nossa. Fica registrada — é o que permite
            // investigar depois — e nada é alterado.
            logger.LogInformation(
                "Notificação {Identificador} do canal {Canal} sem assinatura correspondente.",
                identificador,
                canal);

            return new ResultadoDaNotificacao(true, null);
        }

        AplicarEvento(assinatura, evento, agora);

        await assinaturas.AtualizarSituacaoAsync(assinatura, cancellationToken);

        return new ResultadoDaNotificacao(true, assinatura.Situacao);
    }

    /// <summary>
    /// Traduz o evento do provedor em mudança de estado.
    /// </summary>
    /// <remarks>
    /// Cancelamento não suspende na hora: a pessoa pagou até o fim do período e tem direito a
    /// usá-lo. Encerrar imediatamente seria ficar com o dinheiro e cortar o serviço.
    /// </remarks>
    private static void AplicarEvento(
        AssinaturaEntity assinatura,
        EventoDoProvedor evento,
        DateTimeOffset agora)
    {
        assinatura.AtualizadoEm = agora;

        switch (evento)
        {
            case EventoDoProvedor.Renovacao:
                assinatura.Situacao = SituacaoAssinatura.Ativa;
                assinatura.PeriodoFim = MaiorEntre(assinatura.PeriodoFim, agora).AddMonths(1);
                assinatura.CarenciaAte = null;
                break;

            case EventoDoProvedor.CancelamentoAgendado:
                // Mantém o período pago e fecha a carência: acabou o período, acabou o serviço,
                // sem tolerância — quem cancelou já disse que não vai pagar.
                assinatura.CarenciaAte = assinatura.PeriodoFim;
                assinatura.Situacao = agora <= assinatura.PeriodoFim
                    ? SituacaoAssinatura.Ativa
                    : SituacaoAssinatura.Cancelada;
                break;

            case EventoDoProvedor.Estorno:
            default:
                assinatura.Situacao = SituacaoAssinatura.Suspensa;
                assinatura.CarenciaAte = agora;
                break;
        }
    }

    private static DateTimeOffset MaiorEntre(DateTimeOffset primeira, DateTimeOffset segunda) =>
        primeira > segunda ? primeira : segunda;
}
