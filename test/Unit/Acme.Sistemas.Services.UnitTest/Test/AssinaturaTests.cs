using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Domain.Entities.Assinaturas;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services.V1.Assinaturas;
using Acme.Sistemas.ExternalIntegration.Pagamentos;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

/// <summary>
/// Situação da assinatura: como o tempo a move e o que ela libera.
/// </summary>
public sealed class AssinaturaTests
{
    private static readonly Guid Ana = Guid.CreateVersion7();
    private static readonly Guid Tenant = Guid.CreateVersion7();
    private static readonly Guid PlanoCasal = Guid.CreateVersion7();
    private static readonly DateTimeOffset Agora = new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Agora;
    }

    private sealed class ContextoDeTeste : IContextoRequisicao
    {
        public Guid? UsuarioId { get; set; } = Ana;

        public Guid? TenantId { get; set; } = Tenant;

        public bool SomenteLeitura { get; set; }
    }

    private static AssinaturaEntity Assinatura(
        SituacaoAssinatura situacao,
        DateTimeOffset periodoFim,
        DateTimeOffset? carenciaAte = null) => new()
        {
            Id = Guid.CreateVersion7(),
            UsuarioId = Ana,
            PlanoId = PlanoCasal,
            Situacao = situacao,
            Canal = CanalPagamento.GooglePlay,
            PeriodoFim = periodoFim,
            CarenciaAte = carenciaAte,
            ComprovanteReferencia = "GPA.0000-1111-2222",
            CriadoEm = Agora.AddMonths(-1),
        };

    private static PlanoEntity Plano(int cota) => new()
    {
        Id = PlanoCasal,
        Codigo = "casal",
        Nome = "Casal",
        CotaCompartilhamentos = cota,
        PrecoMensal = 29.90m,
        Ativo = true,
        CriadoEm = Agora.AddYears(-1),
    };

    // ---------------------------------------------------------------- passagem do tempo

    [Fact]
    public void Periodo_pago_em_dia_mantem_a_assinatura_ativa() =>
        Assert.Equal(
            SituacaoAssinatura.Ativa,
            Assinatura(SituacaoAssinatura.Ativa, Agora.AddDays(10))
                .SituacaoEm(Agora, AssinaturaService.Carencia));

    /// <summary>
    /// Vencer sem renovação não suspende: entra em carência, e durante ela a conta opera
    /// inteira. Suspender no dia seguinte tiraria os recursos de quem só teve o cartão negado
    /// numa tentativa.
    /// </summary>
    [Fact]
    public void Periodo_vencido_dentro_da_tolerancia_vira_carencia() =>
        Assert.Equal(
            SituacaoAssinatura.Carencia,
            Assinatura(SituacaoAssinatura.Ativa, Agora.AddDays(-2))
                .SituacaoEm(Agora, AssinaturaService.Carencia));

    [Fact]
    public void Tolerancia_esgotada_suspende() =>
        Assert.Equal(
            SituacaoAssinatura.Suspensa,
            Assinatura(SituacaoAssinatura.Ativa, Agora.AddDays(-30))
                .SituacaoEm(Agora, AssinaturaService.Carencia));

    /// <summary>
    /// A carência gravada pelo provedor prevalece sobre a padrão: quem sabe até quando tolerar
    /// aquele cliente é quem processa o pagamento dele.
    /// </summary>
    [Fact]
    public void Carencia_informada_pelo_provedor_prevalece_sobre_a_padrao()
    {
        var assinatura = Assinatura(
            SituacaoAssinatura.Ativa, Agora.AddDays(-30), carenciaAte: Agora.AddDays(5));

        Assert.Equal(
            SituacaoAssinatura.Carencia,
            assinatura.SituacaoEm(Agora, AssinaturaService.Carencia));
    }

    /// <summary>
    /// Suspensa e cancelada não voltam sozinhas pela passagem do tempo — quem as tira desse
    /// estado é a confirmação de pagamento, não o relógio.
    /// </summary>
    [Theory]
    [InlineData(SituacaoAssinatura.Suspensa)]
    [InlineData(SituacaoAssinatura.Cancelada)]
    public void Estado_terminal_nao_se_altera_pelo_tempo(SituacaoAssinatura situacao) =>
        Assert.Equal(
            situacao,
            Assinatura(situacao, Agora.AddDays(30)).SituacaoEm(Agora, AssinaturaService.Carencia));

    [Theory]
    [InlineData(SituacaoAssinatura.Ativa, true)]
    [InlineData(SituacaoAssinatura.Carencia, true)]
    [InlineData(SituacaoAssinatura.Suspensa, false)]
    [InlineData(SituacaoAssinatura.Cancelada, false)]
    public void Vigente_inclui_a_carencia(SituacaoAssinatura situacao, bool vigente) =>
        Assert.Equal(vigente, Assinatura(situacao, Agora.AddDays(10)).Vigente);

    // ---------------------------------------------------------------- cota

    [Theory]
    [InlineData(3, 1, 2)]
    [InlineData(3, 3, 0)]
    [InlineData(0, 0, 0)]
    public void Cota_disponivel_e_o_que_sobra_do_plano(int total, int usada, int disponivel) =>
        Assert.Equal(
            disponivel,
            new SituacaoDaConta(SituacaoAssinatura.Ativa, "casal", total, usada).CotaDisponivel);

    /// <summary>
    /// Cota nunca fica negativa. Reduzir o plano de quem já concedeu mais do que o novo plano
    /// permite é decisão comercial legítima; devolver número negativo faria a tela mostrar
    /// "-2 disponíveis".
    /// </summary>
    [Fact]
    public void Cota_usada_acima_do_plano_nao_produz_disponivel_negativo() =>
        Assert.Equal(
            0, new SituacaoDaConta(SituacaoAssinatura.Ativa, "casal", 1, 3).CotaDisponivel);

    [Fact]
    public void Conta_sem_assinatura_e_tratada_como_suspensa()
    {
        Assert.False(SituacaoDaConta.SemAssinatura.Vigente);
        Assert.Equal(0, SituacaoDaConta.SemAssinatura.CotaTotal);
    }

    // ---------------------------------------------------------------- serviço

    private static AssinaturaService Servico(
        AssinaturaEntity? assinatura,
        PlanoEntity? plano = null,
        int compartilhamentos = 0,
        IContextoRequisicao? contexto = null,
        AssinaturaEntity? doDono = null,
        IReadOnlyList<IProvedorDePagamento>? provedores = null,
        IMediator? mediator = null) =>
        Servico(
            new Mock<IAssinaturaWriteRepository>(),
            assinatura,
            plano,
            compartilhamentos,
            contexto,
            doDono,
            provedores,
            mediator);

    private static AssinaturaService Servico(
        Mock<IAssinaturaWriteRepository> assinaturas,
        AssinaturaEntity? assinatura,
        PlanoEntity? plano = null,
        int compartilhamentos = 0,
        IContextoRequisicao? contexto = null,
        AssinaturaEntity? doDono = null,
        IReadOnlyList<IProvedorDePagamento>? provedores = null,
        IMediator? mediator = null)
    {
        assinaturas.Setup(r => r.VigenteDoUsuarioAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assinatura);
        assinaturas.Setup(r => r.ContarCompartilhamentosVigentesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(compartilhamentos);
        assinaturas.Setup(r => r.DoDonoDoTenantAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(doDono);

        var planos = new Mock<IPlanoReadRepository>();
        planos.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);
        planos.Setup(r => r.ObterPorCodigoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plano);

        return new AssinaturaService(
            assinaturas.Object,
            planos.Object,
            provedores ?? [],
            contexto ?? new ContextoDeTeste(),
            mediator ?? Mock.Of<IMediator>(),
            new RelogioFixo(),
            NullLogger<AssinaturaService>.Instance);
    }

    [Fact]
    public async Task Situacao_atual_resolve_a_carencia_e_traz_a_cota_do_plano()
    {
        var situacao = await Servico(
                Assinatura(SituacaoAssinatura.Ativa, Agora.AddDays(-2)),
                Plano(cota: 3),
                compartilhamentos: 1)
            .SituacaoAtualAsync(CancellationToken.None);

        Assert.Equal(SituacaoAssinatura.Carencia, situacao.Situacao);
        Assert.True(situacao.Vigente);
        Assert.Equal("casal", situacao.PlanoCodigo);
        Assert.Equal(2, situacao.CotaDisponivel);
    }

    [Fact]
    public async Task Conta_sem_usuario_autenticado_nao_consulta_assinatura()
    {
        var situacao = await Servico(
                Assinatura(SituacaoAssinatura.Ativa, Agora.AddDays(10)),
                Plano(cota: 3),
                contexto: new ContextoDeTeste { UsuarioId = null })
            .SituacaoAtualAsync(CancellationToken.None);

        Assert.Equal(SituacaoDaConta.SemAssinatura, situacao);
    }

    [Fact]
    public async Task Conta_sem_assinatura_devolve_suspensa()
    {
        var situacao = await Servico(assinatura: null).SituacaoAtualAsync(CancellationToken.None);

        Assert.False(situacao.Vigente);
    }

    /// <summary>
    /// Em tenant de terceiro a cota do visitante não faz sentido: contá-la traria os
    /// compartilhamentos de outra pessoa para a tela dele.
    /// </summary>
    [Fact]
    public async Task Em_tenant_de_terceiro_a_cota_nao_e_apurada()
    {
        var situacao = await Servico(
                Assinatura(SituacaoAssinatura.Ativa, Agora.AddDays(10)),
                Plano(cota: 3),
                compartilhamentos: 2,
                contexto: new ContextoDeTeste { SomenteLeitura = true })
            .SituacaoAtualAsync(CancellationToken.None);

        Assert.Equal(0, situacao.CotaUsada);
        Assert.Equal(3, situacao.CotaDisponivel);
    }

    [Fact]
    public async Task Assinatura_sem_plano_conhecido_nao_concede_cota()
    {
        var situacao = await Servico(
                Assinatura(SituacaoAssinatura.Ativa, Agora.AddDays(10)), plano: null)
            .SituacaoAtualAsync(CancellationToken.None);

        Assert.Equal(string.Empty, situacao.PlanoCodigo);
        Assert.Equal(0, situacao.CotaTotal);
    }

    // ---------------------------------------------------------------- dono do tenant

    /// <summary>
    /// A suspensão do dono derruba o acesso compartilhado sem que nada seja gravado — e a
    /// regularização o devolve pelo mesmo motivo. Não há estado a virar nos dois sentidos, e
    /// portanto não há estado para ficar preso no meio do caminho.
    /// </summary>
    [Fact]
    public async Task Dono_com_assinatura_vigente_mantem_o_tenant_acessivel() =>
        Assert.True(
            await Servico(
                    assinatura: null,
                    doDono: Assinatura(SituacaoAssinatura.Ativa, Agora.AddDays(10)))
                .DonoDoTenantVigenteAsync(Tenant, CancellationToken.None));

    [Fact]
    public async Task Dono_com_periodo_vencido_dentro_da_carencia_ainda_libera_o_tenant() =>
        Assert.True(
            await Servico(
                    assinatura: null,
                    doDono: Assinatura(SituacaoAssinatura.Ativa, Agora.AddDays(-2)))
                .DonoDoTenantVigenteAsync(Tenant, CancellationToken.None));

    [Fact]
    public async Task Dono_suspenso_derruba_o_acesso_ao_tenant_compartilhado() =>
        Assert.False(
            await Servico(
                    assinatura: null,
                    doDono: Assinatura(SituacaoAssinatura.Suspensa, Agora.AddDays(10)))
                .DonoDoTenantVigenteAsync(Tenant, CancellationToken.None));

    [Fact]
    public async Task Tenant_sem_assinatura_do_dono_nao_e_acessivel() =>
        Assert.False(
            await Servico(assinatura: null, doDono: null)
                .DonoDoTenantVigenteAsync(Tenant, CancellationToken.None));

    // ---------------------------------------------------------------- catálogo

    [Fact]
    public void Plano_sem_cota_e_configuracao_valida()
    {
        var individual = Plano(cota: 0);

        Assert.Equal(0, individual.CotaCompartilhamentos);
        Assert.True(individual.Ativo);
    }

    [Fact]
    public void Transacao_guarda_o_identificador_do_provedor_e_a_situacao()
    {
        var transacao = new TransacaoPagamentoEntity
        {
            Id = Guid.CreateVersion7(),
            AssinaturaId = Guid.CreateVersion7(),
            UsuarioId = Ana,
            Canal = CanalPagamento.MercadoPago,
            IdentificadorProvedor = "MP-123",
            Valor = 29.90m,
            Situacao = TransacaoPagamentoEntity.SituacaoConfirmada,
            CriadoEm = Agora,
        };

        Assert.Equal("confirmada", transacao.Situacao);
        Assert.NotEqual(TransacaoPagamentoEntity.SituacaoEstornada, transacao.Situacao);
        Assert.NotEqual(TransacaoPagamentoEntity.SituacaoRecusada, transacao.Situacao);
    }

    // ---------------------------------------------------------------- contratação

    /// <summary>
    /// Provedor que responde o que o teste mandar, sem falar com loja nenhuma. O direito nunca
    /// sai do que o aplicativo afirma — sai daqui, do único ponto que confirma pagamento.
    /// </summary>
    private sealed class ProvedorFalso(
        CanalPagamento canal, ConfirmacaoDeCompra confirmacao) : IProvedorDePagamento
    {
        public CanalPagamento Canal => canal;

        public Task<ConfirmacaoDeCompra> ConfirmarAsync(
            string comprovante, CancellationToken cancellationToken) =>
            Task.FromResult(confirmacao);

        public Task<ConfirmacaoDeCompra> ConsultarAsync(
            string referencia, CancellationToken cancellationToken) =>
            Task.FromResult(confirmacao);
    }

    private static ProvedorFalso Confirma(string referencia = "GPA.9999") =>
        new(CanalPagamento.GooglePlay, ConfirmacaoDeCompra.Aceita(referencia, Agora.AddMonths(1)));

    /// <summary>
    /// Primeira contratação: sem assinatura nenhuma, o provedor confirma, e a virada de "conta
    /// tratada como suspensa" para ativa é regularização como qualquer outra — publica o evento
    /// que restabelece os recursos, mesmo que não haja fila a liberar.
    /// </summary>
    [Fact]
    public async Task Primeira_contratacao_ativa_e_publica_a_regularizacao()
    {
        var mediator = new Mock<IMediator>();

        var resultado = await Servico(
                new Mock<IAssinaturaWriteRepository>(),
                assinatura: null,
                plano: Plano(cota: 3),
                provedores: [Confirma()],
                mediator: mediator.Object)
            .ContratarAsync("casal", CanalPagamento.GooglePlay, "recibo", CancellationToken.None);

        Assert.True(resultado.Sucesso);
        mediator.Verify(
            m => m.PublishAsync(
                It.Is<AssinaturaRegularizada>(e => e.UsuarioId == Ana),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Suspensa que volta a pagar reaproveita a linha e dispara a regularização. Chegar aqui já
    /// significa que estava suspensa: a recusa por "já vigente" acontece antes.
    /// </summary>
    [Fact]
    public async Task Contratar_sobre_suspensa_reativa_e_regulariza()
    {
        var repositorio = new Mock<IAssinaturaWriteRepository>();
        var mediator = new Mock<IMediator>();

        var resultado = await Servico(
                repositorio,
                assinatura: Assinatura(SituacaoAssinatura.Suspensa, Agora.AddDays(-30)),
                plano: Plano(cota: 3),
                provedores: [Confirma()],
                mediator: mediator.Object)
            .ContratarAsync("casal", CanalPagamento.GooglePlay, "recibo", CancellationToken.None);

        Assert.True(resultado.Sucesso);
        repositorio.Verify(
            r => r.AtualizarSituacaoAsync(
                It.Is<AssinaturaEntity>(a => a.Situacao == SituacaoAssinatura.Ativa),
                It.IsAny<CancellationToken>()),
            Times.Once);
        mediator.Verify(
            m => m.PublishAsync(It.IsAny<AssinaturaRegularizada>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Assinatura viva não recontrata: cobraria duas vezes por um produto que entrega uma. Nenhum
    /// direito novo, nenhuma regularização.
    /// </summary>
    [Fact]
    public async Task Segunda_contratacao_com_assinatura_vigente_e_recusada()
    {
        var repositorio = new Mock<IAssinaturaWriteRepository>();
        var mediator = new Mock<IMediator>();

        var resultado = await Servico(
                repositorio,
                assinatura: Assinatura(SituacaoAssinatura.Ativa, Agora.AddDays(20)),
                plano: Plano(cota: 3),
                provedores: [Confirma()],
                mediator: mediator.Object)
            .ContratarAsync("casal", CanalPagamento.GooglePlay, "recibo", CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(AssinaturaService.CodigoJaAssinante, resultado.Erro!.Codigo);
        repositorio.Verify(
            r => r.CriarAsync(It.IsAny<AssinaturaEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mediator.Verify(
            m => m.PublishAsync(It.IsAny<AssinaturaRegularizada>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Comprovante que o provedor não confirma não concede direito nenhum, e o estado anterior
    /// fica intacto. Vale também para o provedor fora do ar.
    /// </summary>
    [Fact]
    public async Task Comprovante_nao_confirmado_recusa_e_nao_grava()
    {
        var repositorio = new Mock<IAssinaturaWriteRepository>();
        var mediator = new Mock<IMediator>();

        var provedor = new ProvedorFalso(
            CanalPagamento.GooglePlay, ConfirmacaoDeCompra.Recusada("nao_pago"));

        var resultado = await Servico(
                repositorio,
                assinatura: null,
                plano: Plano(cota: 3),
                provedores: [provedor],
                mediator: mediator.Object)
            .ContratarAsync("casal", CanalPagamento.GooglePlay, "recibo", CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(AssinaturaService.CodigoComprovanteNaoConfirmado, resultado.Erro!.Codigo);
        repositorio.Verify(
            r => r.CriarAsync(It.IsAny<AssinaturaEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mediator.Verify(
            m => m.PublishAsync(It.IsAny<AssinaturaRegularizada>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// O mesmo comprovante não ativa duas contas do aplicativo. É a regra que impede comprar uma
    /// vez e liberar a família inteira, e vale mesmo quando a outra conta é do mesmo titular na
    /// loja: a assinatura é da conta, não da pessoa.
    /// </summary>
    [Fact]
    public async Task Comprovante_ja_vinculado_a_outra_conta_e_recusado()
    {
        var repositorio = new Mock<IAssinaturaWriteRepository>();

        var deOutraConta = Assinatura(SituacaoAssinatura.Ativa, Agora.AddDays(20));
        deOutraConta.UsuarioId = Guid.CreateVersion7();

        repositorio.Setup(r => r.PorReferenciaAsync(
                It.IsAny<CanalPagamento>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deOutraConta);

        var resultado = await Servico(
                repositorio,
                assinatura: null,
                plano: Plano(cota: 3),
                provedores: [Confirma("GPA.duplicado")])
            .ContratarAsync("casal", CanalPagamento.GooglePlay, "recibo", CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(AssinaturaService.CodigoComprovanteEmOutraConta, resultado.Erro!.Codigo);
        repositorio.Verify(
            r => r.CriarAsync(It.IsAny<AssinaturaEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Sem titular identificado não há a quem conceder direito.</summary>
    [Fact]
    public async Task Contratar_sem_usuario_autenticado_e_recusado()
    {
        var resultado = await Servico(
                new Mock<IAssinaturaWriteRepository>(),
                assinatura: null,
                plano: Plano(cota: 3),
                contexto: new ContextoDeTeste { UsuarioId = null },
                provedores: [Confirma()])
            .ContratarAsync("casal", CanalPagamento.GooglePlay, "recibo", CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(AssinaturaService.CodigoSemUsuario, resultado.Erro!.Codigo);
    }

    /// <summary>Plano que não existe, ou desativado, não é contratável.</summary>
    [Fact]
    public async Task Contratar_plano_inexistente_e_recusado()
    {
        var resultado = await Servico(
                new Mock<IAssinaturaWriteRepository>(),
                assinatura: null,
                plano: null,
                provedores: [Confirma()])
            .ContratarAsync("fantasma", CanalPagamento.GooglePlay, "recibo", CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(AssinaturaService.CodigoPlanoInexistente, resultado.Erro!.Codigo);
    }

    /// <summary>Sem provedor para o canal pedido, a contratação não acontece.</summary>
    [Fact]
    public async Task Canal_sem_provedor_registrado_e_recusado()
    {
        var resultado = await Servico(
                new Mock<IAssinaturaWriteRepository>(),
                assinatura: null,
                plano: Plano(cota: 3),
                provedores: [Confirma()]) // provê GooglePlay, pedem MercadoPago
            .ContratarAsync("casal", CanalPagamento.MercadoPago, "recibo", CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(AssinaturaService.CodigoCanalIndisponivel, resultado.Erro!.Codigo);
    }

    /// <summary>
    /// Provedor fora do ar cai como canal indisponível, não como comprovante recusado: manter o
    /// estado anterior é seguro dos dois lados, e a mensagem não acusa a compra da pessoa por uma
    /// falha que é do provedor.
    /// </summary>
    [Fact]
    public async Task Provedor_indisponivel_recusa_como_canal_indisponivel()
    {
        var provedor = new ProvedorFalso(
            CanalPagamento.GooglePlay, ConfirmacaoDeCompra.Recusada("provedor_indisponivel"));

        var resultado = await Servico(
                new Mock<IAssinaturaWriteRepository>(),
                assinatura: null,
                plano: Plano(cota: 3),
                provedores: [provedor])
            .ContratarAsync("casal", CanalPagamento.GooglePlay, "recibo", CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(AssinaturaService.CodigoCanalIndisponivel, resultado.Erro!.Codigo);
    }

    // ---------------------------------------------------------------- notificações

    private static AssinaturaService ServicoDeNotificacao(
        Mock<IAssinaturaWriteRepository> repositorio,
        AssinaturaEntity? porReferencia,
        bool notificacaoInedita,
        IMediator mediator)
    {
        repositorio.Setup(r => r.RegistrarNotificacaoAsync(
                It.IsAny<CanalPagamento>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notificacaoInedita);
        repositorio.Setup(r => r.PorReferenciaAsync(
                It.IsAny<CanalPagamento>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(porReferencia);

        return Servico(repositorio, assinatura: null, mediator: mediator);
    }

    /// <summary>
    /// A idempotência é do identificador do provedor: ele reenvia até receber confirmação, e
    /// processar um estorno duas vezes suspenderia uma assinatura já regularizada no meio tempo.
    /// </summary>
    [Fact]
    public async Task Notificacao_repetida_nao_reaplica_nem_consulta()
    {
        var repositorio = new Mock<IAssinaturaWriteRepository>();
        var mediator = new Mock<IMediator>();

        var servico = ServicoDeNotificacao(
            repositorio, porReferencia: null, notificacaoInedita: false, mediator.Object);

        var resultado = await servico.AplicarNotificacaoAsync(
            CanalPagamento.GooglePlay, "evt-1", EventoDoProvedor.Renovacao, "GPA.1", "{}",
            CancellationToken.None);

        Assert.False(resultado.Processada);
        repositorio.Verify(
            r => r.PorReferenciaAsync(
                It.IsAny<CanalPagamento>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repositorio.Verify(
            r => r.AtualizarSituacaoAsync(It.IsAny<AssinaturaEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Renovação que chega depois da suspensão é regularização: o usuário pagou e não vai pedir
    /// nada além disso. Restabelece e publica o evento.
    /// </summary>
    [Fact]
    public async Task Renovacao_sobre_suspensa_reativa_e_publica_regularizacao()
    {
        var repositorio = new Mock<IAssinaturaWriteRepository>();
        var mediator = new Mock<IMediator>();

        var servico = ServicoDeNotificacao(
            repositorio,
            porReferencia: Assinatura(SituacaoAssinatura.Suspensa, Agora.AddDays(-30)),
            notificacaoInedita: true,
            mediator.Object);

        var resultado = await servico.AplicarNotificacaoAsync(
            CanalPagamento.GooglePlay, "evt-2", EventoDoProvedor.Renovacao, "GPA.1", "{}",
            CancellationToken.None);

        Assert.True(resultado.Processada);
        Assert.Equal(SituacaoAssinatura.Ativa, resultado.SituacaoResultante);
        mediator.Verify(
            m => m.PublishAsync(It.IsAny<AssinaturaRegularizada>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Renovação sobre quem já estava vigente não regulariza nada — não houve recurso
    /// restabelecido —, e disparar o evento varreria a fila retida todo mês sem ter o que varrer.
    /// </summary>
    [Fact]
    public async Task Renovacao_sobre_vigente_atualiza_mas_nao_republica()
    {
        var repositorio = new Mock<IAssinaturaWriteRepository>();
        var mediator = new Mock<IMediator>();

        var servico = ServicoDeNotificacao(
            repositorio,
            porReferencia: Assinatura(SituacaoAssinatura.Ativa, Agora.AddDays(5)),
            notificacaoInedita: true,
            mediator.Object);

        await servico.AplicarNotificacaoAsync(
            CanalPagamento.GooglePlay, "evt-3", EventoDoProvedor.Renovacao, "GPA.1", "{}",
            CancellationToken.None);

        mediator.Verify(
            m => m.PublishAsync(It.IsAny<AssinaturaRegularizada>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Estorno suspende na hora e não é regularização. Nada de evento.
    /// </summary>
    [Fact]
    public async Task Estorno_suspende_e_nao_publica_regularizacao()
    {
        var repositorio = new Mock<IAssinaturaWriteRepository>();
        var mediator = new Mock<IMediator>();

        var servico = ServicoDeNotificacao(
            repositorio,
            porReferencia: Assinatura(SituacaoAssinatura.Ativa, Agora.AddDays(20)),
            notificacaoInedita: true,
            mediator.Object);

        var resultado = await servico.AplicarNotificacaoAsync(
            CanalPagamento.GooglePlay, "evt-4", EventoDoProvedor.Estorno, "GPA.1", "{}",
            CancellationToken.None);

        Assert.Equal(SituacaoAssinatura.Suspensa, resultado.SituacaoResultante);
        mediator.Verify(
            m => m.PublishAsync(It.IsAny<AssinaturaRegularizada>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Cancelamento na loja vale até o fim do período pago: a pessoa pagou e tem direito de usar.
    /// Dentro do período segue ativa, com a carência fechada no fim dele; não regulariza nada.
    /// </summary>
    [Fact]
    public async Task Cancelamento_agendado_mantem_ativa_ate_o_fim_do_periodo()
    {
        var repositorio = new Mock<IAssinaturaWriteRepository>();
        var mediator = new Mock<IMediator>();

        var servico = ServicoDeNotificacao(
            repositorio,
            porReferencia: Assinatura(SituacaoAssinatura.Ativa, Agora.AddDays(20)),
            notificacaoInedita: true,
            mediator.Object);

        var resultado = await servico.AplicarNotificacaoAsync(
            CanalPagamento.GooglePlay, "evt-cancel", EventoDoProvedor.CancelamentoAgendado,
            "GPA.1", "{}", CancellationToken.None);

        Assert.Equal(SituacaoAssinatura.Ativa, resultado.SituacaoResultante);
        repositorio.Verify(
            r => r.AtualizarSituacaoAsync(
                It.Is<AssinaturaEntity>(a => a.CarenciaAte == a.PeriodoFim),
                It.IsAny<CancellationToken>()),
            Times.Once);
        mediator.Verify(
            m => m.PublishAsync(It.IsAny<AssinaturaRegularizada>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Cancelamento cujo período já se encerrou fecha a assinatura de vez.
    /// </summary>
    [Fact]
    public async Task Cancelamento_de_periodo_ja_vencido_encerra()
    {
        var repositorio = new Mock<IAssinaturaWriteRepository>();
        var mediator = new Mock<IMediator>();

        var servico = ServicoDeNotificacao(
            repositorio,
            porReferencia: Assinatura(SituacaoAssinatura.Carencia, Agora.AddDays(-1)),
            notificacaoInedita: true,
            mediator.Object);

        var resultado = await servico.AplicarNotificacaoAsync(
            CanalPagamento.GooglePlay, "evt-cancel-2", EventoDoProvedor.CancelamentoAgendado,
            "GPA.1", "{}", CancellationToken.None);

        Assert.Equal(SituacaoAssinatura.Cancelada, resultado.SituacaoResultante);
    }

    /// <summary>
    /// Notificação de assinatura que não é nossa fica registrada — permite investigar depois — e
    /// nada é alterado.
    /// </summary>
    [Fact]
    public async Task Notificacao_sem_assinatura_correspondente_nada_altera()
    {
        var repositorio = new Mock<IAssinaturaWriteRepository>();
        var mediator = new Mock<IMediator>();

        var servico = ServicoDeNotificacao(
            repositorio, porReferencia: null, notificacaoInedita: true, mediator.Object);

        var resultado = await servico.AplicarNotificacaoAsync(
            CanalPagamento.GooglePlay, "evt-5", EventoDoProvedor.Renovacao, "GPA.desconhecida",
            "{}", CancellationToken.None);

        Assert.True(resultado.Processada);
        Assert.Null(resultado.SituacaoResultante);
        repositorio.Verify(
            r => r.AtualizarSituacaoAsync(It.IsAny<AssinaturaEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mediator.Verify(
            m => m.PublishAsync(It.IsAny<AssinaturaRegularizada>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
