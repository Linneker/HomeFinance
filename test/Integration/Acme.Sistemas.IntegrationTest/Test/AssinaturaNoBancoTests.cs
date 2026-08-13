using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Core.Mediators.Notification;
using Acme.Sistemas.Domain.Entities.Assinaturas;
using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.ExternalIntegration.Pagamentos;
using Acme.Sistemas.IntegrationTest.Config;
using Acme.Sistemas.Repository.Repositories.V1.Alerta;
using Acme.Sistemas.Repository.Repositories.V1.Assinatura;
using Acme.Sistemas.Repository.Repositories.V1.DocumentoRetido;
using Acme.Sistemas.Repository.Repositories.V1.Tenant;
using Acme.Sistemas.Repository.Repositories.V1.Usuario;
using Acme.Sistemas.Services.V1.Assinaturas;
using Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Acme.Sistemas.IntegrationTest.Test;

/// <summary>
/// A assinatura decide acesso, e as três decisões que só o banco prova: a suspensão do dono
/// derruba o acesso compartilhado sem tocar na RLS, a regularização o devolve e libera a fila
/// retida sem ação do usuário, e a reconciliação corrige o que a notificação perdida deixou
/// divergente.
/// </summary>
/// <remarks>
/// Os testes unitários provam cada decisão contra dublês. O que eles não alcançam é a assimetria
/// que sustenta o compartilhamento: a policy de <c>SELECT</c> deixa o convidado ler o tenant do
/// dono — é a assinatura do dono, calculada e nunca gravada, que barra a abertura. Trocar essa
/// verificação por RLS pareceria seguro num teste de unidade e quebraria só aqui.
/// </remarks>
[Collection(nameof(BancoCollection))]
public sealed class AssinaturaNoBancoTests(AmbienteBanco ambiente)
{
    private static CancellationToken Ct => CancellationToken.None;

    private static readonly Guid PlanoCasal = new("0199a000-0000-7000-8000-000000000002");

    private sealed record Conta(Guid UsuarioId, Guid TenantId, string Email);

    /// <summary>
    /// Provedor que responde o que o teste mandar. Nenhum direito sai do que o aplicativo afirma —
    /// sai daqui, o único ponto que confirma pagamento.
    /// </summary>
    private sealed class ProvedorFalso(CanalPagamento canal, bool confirma) : IProvedorDePagamento
    {
        public CanalPagamento Canal => canal;

        private Task<ConfirmacaoDeCompra> Responder() =>
            Task.FromResult(confirma
                ? ConfirmacaoDeCompra.Aceita($"{canal}.OK", DateTimeOffset.UtcNow.AddMonths(1))
                : ConfirmacaoDeCompra.Recusada("cancelada_na_loja"));

        public Task<ConfirmacaoDeCompra> ConfirmarAsync(string c, CancellationToken _) => Responder();

        public Task<ConfirmacaoDeCompra> ConsultarAsync(string r, CancellationToken _) => Responder();
    }

    private async Task<Conta> CriarContaAsync(ContextoFixo contexto)
    {
        var usuarioId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var email = $"{usuarioId:N}@teste.local";
        var agora = DateTimeOffset.UtcNow;

        contexto.UsuarioId = null;
        contexto.TenantId = null;
        contexto.SomenteLeitura = false;

        await new UsuarioWriteRepository(ambiente.CriarGerenciador(contexto)).CriarContaAsync(
            new UsuarioEntity { Id = usuarioId, Email = email, SenhaHash = "hash", CriadoEm = agora },
            new TenantEntity { Id = tenantId, UsuarioId = usuarioId, CriadoEm = agora },
            [.. DocumentoConsentimento.Obrigatorios.Select(documento => new ConsentimentoEntity
            {
                Id = Guid.CreateVersion7(),
                UsuarioId = usuarioId,
                Documento = documento,
                Versao = "1.0",
                AceitoEm = agora,
            })],
            Ct);

        return new Conta(usuarioId, tenantId, email);
    }

    private static void Ativar(ContextoFixo contexto, Conta conta, bool somenteLeitura = false)
    {
        contexto.UsuarioId = conta.UsuarioId;
        contexto.TenantId = conta.TenantId;
        contexto.SomenteLeitura = somenteLeitura;
    }

    /// <summary>Grava uma assinatura para a conta, sob o contexto dela — a policy de INSERT exige.</summary>
    private async Task<AssinaturaEntity> AssinarAsync(
        ContextoFixo contexto,
        Conta conta,
        SituacaoAssinatura situacao,
        DateTimeOffset periodoFim,
        DateTimeOffset? carenciaAte = null,
        CanalPagamento canal = CanalPagamento.GooglePlay,
        string? referencia = null)
    {
        Ativar(contexto, conta);

        var assinatura = new AssinaturaEntity
        {
            Id = Guid.CreateVersion7(),
            UsuarioId = conta.UsuarioId,
            PlanoId = PlanoCasal,
            Situacao = situacao,
            Canal = canal,
            PeriodoFim = periodoFim,
            CarenciaAte = carenciaAte,
            ComprovanteReferencia = referencia ?? $"{canal}.{Guid.CreateVersion7():N}",
            CriadoEm = DateTimeOffset.UtcNow.AddMonths(-1),
        };

        await new AssinaturaWriteRepository(ambiente.CriarGerenciador(contexto))
            .CriarAsync(assinatura, Ct);

        return assinatura;
    }

    private AssinaturaService Servico(
        ContextoFixo contexto,
        IMediator? mediator = null,
        params IProvedorDePagamento[] provedores)
    {
        var gerenciador = ambiente.CriarGerenciador(contexto);

        return new AssinaturaService(
            new AssinaturaWriteRepository(gerenciador),
            new AssinaturaReadRepository(gerenciador),
            provedores,
            contexto,
            mediator ?? Mock.Of<IMediator>(),
            TimeProvider.System,
            NullLogger<AssinaturaService>.Instance);
    }

    /// <summary>
    /// Concede o acesso pelo Command real, com a cota vinda da assinatura de verdade — não de um
    /// dublê. Devolve o acesso resolvido, como o middleware faria.
    /// </summary>
    private async Task CompartilharAsync(ContextoFixo contexto, Conta dono, Conta destinatario)
    {
        Ativar(contexto, dono);

        var concessao = await new CompartilharTenantCommandHandler(
                new TenantWriteRepository(ambiente.CriarGerenciador(contexto)),
                Servico(contexto),
                TimeProvider.System)
            .HandleAsync(
                new CompartilharTenantCommand(
                    dono.UsuarioId, dono.TenantId, destinatario.Email,
                    ConsentimentoConfirmado: true, IncluiScr: false),
                Ct);

        Assert.True(concessao.Sucesso);
    }

    // ---------------------------------------------------------------- 11.22

    /// <summary>
    /// Ana suspende e Bruno perde o tenant dela — mas continua no próprio. A RLS ainda entrega a
    /// linha da Ana ao Bruno; quem barra a abertura é a assinatura do dono, não a policy. O teste
    /// resolve o acesso pelo banco antes de negar pela assinatura, para que remover a verificação
    /// não possa parecer seguro.
    /// </summary>
    [Fact]
    public async Task Suspensao_do_dono_derruba_o_compartilhado_e_preserva_o_tenant_do_destinatario()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);
        var bruno = await CriarContaAsync(contexto);

        await AssinarAsync(contexto, ana, SituacaoAssinatura.Ativa, DateTimeOffset.UtcNow.AddDays(20));
        await AssinarAsync(contexto, bruno, SituacaoAssinatura.Ativa, DateTimeOffset.UtcNow.AddDays(20));

        await CompartilharAsync(contexto, ana, bruno);

        // Com Ana ativa, Bruno abre o tenant dela.
        Ativar(contexto, bruno, somenteLeitura: true);
        contexto.TenantId = null;

        Assert.True(await Servico(contexto).DonoDoTenantVigenteAsync(ana.TenantId, Ct));

        // Ana deixa de pagar: período vencido e carência esgotada.
        Ativar(contexto, ana);
        var assinaturaAna = await new AssinaturaWriteRepository(ambiente.CriarGerenciador(contexto))
            .VigenteDoUsuarioAsync(ana.UsuarioId, Ct);
        assinaturaAna!.Situacao = SituacaoAssinatura.Suspensa;
        assinaturaAna.CarenciaAte = DateTimeOffset.UtcNow.AddDays(-1);
        assinaturaAna.AtualizadoEm = DateTimeOffset.UtcNow;
        await new AssinaturaWriteRepository(ambiente.CriarGerenciador(contexto))
            .AtualizarSituacaoAsync(assinaturaAna, Ct);

        // Bruno agora não abre o tenant da Ana...
        Ativar(contexto, bruno, somenteLeitura: true);
        contexto.TenantId = null;

        Assert.False(await Servico(contexto).DonoDoTenantVigenteAsync(ana.TenantId, Ct));

        // ...mas a RLS ainda lhe entrega a linha: a recusa é da assinatura, não da policy.
        var acesso = await new TenantWriteRepository(ambiente.CriarGerenciador(contexto))
            .ResolverAcessoAsync(bruno.UsuarioId, ana.TenantId, Ct);
        Assert.NotNull(acesso);
        Assert.False(acesso.EhProprio);

        // E o tenant próprio do Bruno segue operante: assinatura dele vigente e acesso próprio.
        Ativar(contexto, bruno);
        Assert.True((await Servico(contexto).SituacaoAtualAsync(Ct)).Vigente);

        var proprio = await new TenantWriteRepository(ambiente.CriarGerenciador(contexto))
            .ResolverAcessoAsync(bruno.UsuarioId, bruno.TenantId, Ct);
        Assert.NotNull(proprio);
        Assert.True(proprio.EhProprio);
        Assert.False(proprio.SomenteLeitura);
    }

    /// <summary>
    /// Abrir um tenant compartilhado exige as duas assinaturas vigentes. Com o dono em dia mas o
    /// próprio destinatário suspenso, a abertura é barrada pela assinatura <em>dele</em> — os dois
    /// motivos são distintos porque quem precisa pagar é uma pessoa diferente em cada caso.
    /// </summary>
    [Fact]
    public async Task Destinatario_sem_assinatura_propria_ativa_nao_abre_o_compartilhado()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);
        var bruno = await CriarContaAsync(contexto);

        await AssinarAsync(contexto, ana, SituacaoAssinatura.Ativa, DateTimeOffset.UtcNow.AddDays(20));
        await AssinarAsync(
            contexto, bruno, SituacaoAssinatura.Suspensa,
            DateTimeOffset.UtcNow.AddDays(-40), DateTimeOffset.UtcNow.AddDays(-1));

        await CompartilharAsync(contexto, ana, bruno);

        // O dono está em dia — não é ele que barra.
        Ativar(contexto, bruno, somenteLeitura: true);
        contexto.TenantId = null;
        Assert.True(await Servico(contexto).DonoDoTenantVigenteAsync(ana.TenantId, Ct));

        // Mas a assinatura do próprio Bruno não está vigente: é o que o middleware verifica antes
        // de abrir o tenant de terceiro.
        Ativar(contexto, bruno);
        Assert.False((await Servico(contexto).SituacaoAtualAsync(Ct)).Vigente);
    }

    // ---------------------------------------------------------------- 11.23

    /// <summary>
    /// A regularização restabelece o acesso e libera a fila retida no mesmo instante do pagamento,
    /// sem ação adicional. O disparo é por evento: quem paga não pede nada além disso, e o efeito
    /// não pode esperar o próximo ciclo de uma rotina.
    /// </summary>
    [Fact]
    public async Task Regularizacao_do_dono_restaura_acesso_e_libera_a_fila_sem_acao_do_usuario()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);

        await AssinarAsync(
            contexto, ana, SituacaoAssinatura.Suspensa,
            DateTimeOffset.UtcNow.AddDays(-40), DateTimeOffset.UtcNow.AddDays(-1));

        // Um documento enviado durante a suspensão fica retido e pendente.
        Ativar(contexto, ana);
        var documentos = new DocumentoRetidoRepository(ambiente.CriarGerenciador(contexto));
        await documentos.ReterAsync(
            new DocumentoRetidoEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = ana.TenantId,
                Referencia = "s3://retido/nota.pdf",
                Tipo = "fatura",
                CriadoEm = DateTimeOffset.UtcNow,
                ProcessadoEm = null,
            },
            Ct);

        Assert.Single(await documentos.PendentesAsync(Ct));
        Assert.False(await Servico(contexto).DonoDoTenantVigenteAsync(ana.TenantId, Ct));

        // A fila usa um serviço próprio (mediator neutro) para consultar a situação: quebra o
        // ciclo serviço → evento → handler → fila → serviço sem afrouxar nada do caminho real.
        var fila = new FilaDeDocumentosRetidos(
            new DocumentoRetidoRepository(ambiente.CriarGerenciador(contexto)),
            Servico(contexto),
            TimeProvider.System,
            NullLogger<FilaDeDocumentosRetidos>.Instance);

        var handler = new LiberarFilaRetidaAoRegularizar(
            new RotinaReadRepository(ambiente.CriarGerenciador(contexto)),
            contexto,
            fila,
            NullLogger<LiberarFilaRetidaAoRegularizar>.Instance);

        var mediator = new Mediator(
            new ServiceCollection()
                .AddSingleton<INotificationHandler<AssinaturaRegularizada>>(handler)
                .BuildServiceProvider());

        // Ana paga. Só isto — a contratação dispara a regularização.
        var servico = Servico(contexto, mediator, new ProvedorFalso(CanalPagamento.GooglePlay, confirma: true));

        var resultado = await servico.ContratarAsync(
            "casal", CanalPagamento.GooglePlay, "recibo-da-regularizacao", Ct);

        Assert.True(resultado.Sucesso);

        // Sem nenhum outro passo: acesso de volta e fila vazia.
        Assert.True(await Servico(contexto).DonoDoTenantVigenteAsync(ana.TenantId, Ct));
        Assert.Empty(await new DocumentoRetidoRepository(ambiente.CriarGerenciador(contexto))
            .PendentesAsync(Ct));
    }

    // ---------------------------------------------------------------- 11.24

    /// <summary>
    /// A renovação cuja notificação se perdeu deixou a conta suspensa indevidamente. A
    /// reconciliação pergunta ao provedor e corrige — sem consulta ativa, a conta ficaria
    /// bloqueada por tempo indeterminado, e ninguém abre chamado por estar pagando e sem acesso
    /// que só a loja sabe estar em dia.
    /// </summary>
    [Fact]
    public async Task Reconciliacao_corrige_a_assinatura_suspensa_que_o_provedor_confirma_ativa()
    {
        var contexto = new ContextoFixo();

        var conta = await CriarContaAsync(contexto);

        // Canal próprio (App Store) e provedor só desse canal: a reconciliação varre todas as
        // vigentes da base, mas só corrige as do canal que sabe consultar — as demais ficam como
        // "sem provedor" e intactas, o que isola este teste das assinaturas dos outros.
        await AssinarAsync(
            contexto, conta, SituacaoAssinatura.Suspensa,
            DateTimeOffset.UtcNow.AddDays(-10),
            carenciaAte: DateTimeOffset.UtcNow.AddDays(-3),
            canal: CanalPagamento.AppStore);

        var rotina = new RotinaDeReconciliacao(
            new AssinaturaWriteRepository(ambiente.CriarGerenciador(contexto)),
            [new ProvedorFalso(CanalPagamento.AppStore, confirma: true)],
            TimeProvider.System,
            NullLogger<RotinaDeReconciliacao>.Instance);

        var resultado = await rotina.ExecutarAsync(Ct);

        Assert.True(resultado.Corrigidas >= 1);

        Ativar(contexto, conta);
        var reconciliada = await new AssinaturaWriteRepository(ambiente.CriarGerenciador(contexto))
            .VigenteDoUsuarioAsync(conta.UsuarioId, Ct);

        Assert.NotNull(reconciliada);
        Assert.Equal(SituacaoAssinatura.Ativa, reconciliada.Situacao);
    }
}
