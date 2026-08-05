using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Entities.Planejamento;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.CopiarOrcamento;

/// <summary>
/// Replica o orçamento de um mês em outro.
/// </summary>
/// <remarks>
/// A confirmação é pedida devolvendo <see cref="CopiarOrcamentoCommandResult.RequerConfirmacao"/>
/// e não gravando nada. O cliente reenvia a mesma requisição com o aceite. Ter a decisão do
/// lado de quem chama, e não um parâmetro "forçar" ligado por padrão, é o que impede a cópia de
/// apagar em silêncio um mês que a pessoa já tinha ajustado à mão.
/// </remarks>
public sealed class CopiarOrcamentoCommandHandler(
    IOrcamentoWriteRepository orcamentos,
    IFonteRendaWriteRepository fontes,
    TimeProvider relogio)
    : IRequestHandler<CopiarOrcamentoCommand, CopiarOrcamentoCommandResult>
{
    public const string CodigoOrigemVazia = "orcamento_origem_vazio";
    public const string CodigoConfirmacaoNecessaria = "confirmacao_necessaria";

    public async Task<CopiarOrcamentoCommandResult> HandleAsync(
        CopiarOrcamentoCommand request,
        CancellationToken cancellationToken)
    {
        var erros = CopiarOrcamentoCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return CopiarOrcamentoCommandResult.Recusado(erros[0]);
        }

        var origem = CalendarioFinanceiro.PrimeiroDiaDoMes(request.Origem);
        var destino = CalendarioFinanceiro.PrimeiroDiaDoMes(request.Destino);

        var doOrigem = await orcamentos.ListarDoMesAsync(origem, cancellationToken);

        // Origem vazia apagaria o destino sem colocar nada no lugar. Recusar é o comportamento
        // seguro: quem quer zerar um mês remove as linhas, não copia o vazio por cima.
        if (doOrigem.Count == 0)
        {
            return CopiarOrcamentoCommandResult.Recusado(
                new Error(
                    CodigoOrigemVazia,
                    "O mês de origem não tem orçamento definido.",
                    nameof(request.Origem)));
        }

        var doDestino = await orcamentos.ListarDoMesAsync(destino, cancellationToken);

        if (doDestino.Count > 0 && !request.Confirmado)
        {
            return CopiarOrcamentoCommandResult.ConfirmacaoNecessaria(
                destino,
                doDestino.Count,
                new Error(
                    CodigoConfirmacaoNecessaria,
                    $"O mês de destino já tem {doDestino.Count} categoria(s) orçada(s). " +
                    "Confirme para substituir."));
        }

        var agora = relogio.GetUtcNow();

        var copias = doOrigem
            .Select(item => new OrcamentoMensalEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = item.TenantId,
                CategoriaId = item.CategoriaId,
                Competencia = destino,
                ValorPlanejado = item.ValorPlanejado,
                CriadoEm = agora,
            })
            .ToList();

        var linhas = await orcamentos.SubstituirMesAsync(destino, copias, cancellationToken);

        var vigentes = await fontes.ListarAsync(destino, cancellationToken);
        var total = copias.Sum(item => item.ValorPlanejado);
        var renda = PrevisaoDeRenda.DoMes(destino, vigentes);

        // A renda do destino pode ser outra — é comum copiar para um mês em que o salário já
        // mudou. Avaliar contra a renda do destino, e não a da origem, é o que faz o aviso
        // aparecer justamente quando o plano herdado deixou de caber.
        return CopiarOrcamentoCommandResult.Copiado(
            destino, linhas, total, renda, PrevisaoDeRenda.Avaliar(renda, total)?.Mensagem);
    }
}
