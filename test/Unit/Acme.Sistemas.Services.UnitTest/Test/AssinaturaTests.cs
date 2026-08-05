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
        IReadOnlyList<IProvedorDePagamento>? provedores = null)
    {
        var assinaturas = new Mock<IAssinaturaWriteRepository>();

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

        return new AssinaturaService(
            assinaturas.Object,
            planos.Object,
            provedores ?? [],
            contexto ?? new ContextoDeTeste(),
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
}
