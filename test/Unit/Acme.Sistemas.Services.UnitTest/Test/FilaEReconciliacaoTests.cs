using Acme.Sistemas.Domain.Entities.Assinaturas;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.ExternalIntegration.Pagamentos;
using Acme.Sistemas.Services.V1.Alertas;
using Acme.Sistemas.Services.V1.Assinaturas;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

/// <summary>
/// A fila de documentos retidos e a reconciliação periódica: as duas decisões que a suspensão
/// exige e que a integração prova contra o banco — aqui, a lógica de cada uma isolada.
/// </summary>
public sealed class FilaEReconciliacaoTests
{
    private static readonly Guid Tenant = Guid.CreateVersion7();
    private static readonly DateTimeOffset Agora = new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Agora;
    }

    private static IAssinaturaService ComSituacao(SituacaoAssinatura situacao)
    {
        var assinaturas = new Mock<IAssinaturaService>();
        assinaturas.Setup(a => a.SituacaoAtualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SituacaoDaConta(situacao, "casal", 3, 0));
        return assinaturas.Object;
    }

    private sealed class ProvedorFalso(
        CanalPagamento canal, ConfirmacaoDeCompra confirmacao) : IProvedorDePagamento
    {
        public CanalPagamento Canal => canal;

        public Task<ConfirmacaoDeCompra> ConfirmarAsync(string c, CancellationToken _) =>
            Task.FromResult(confirmacao);

        public Task<ConfirmacaoDeCompra> ConsultarAsync(string r, CancellationToken _) =>
            Task.FromResult(confirmacao);
    }

    private static DocumentoRetidoEntity Documento() => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Tenant,
        Referencia = "s3://retido/x.pdf",
        Tipo = "fatura",
        CriadoEm = Agora,
        ProcessadoEm = null,
    };

    // ---------------------------------------------------------------- fila retida

    /// <summary>
    /// Com assinatura suspensa o documento é armazenado, marcado como pendente, e nada é
    /// processado: <see cref="FilaDeDocumentosRetidos.ReceberAsync"/> devolve "não vigente".
    /// </summary>
    [Fact]
    public async Task Documento_recebido_com_assinatura_suspensa_fica_pendente()
    {
        var documentos = new Mock<IDocumentoRetidoRepository>();

        var fila = new FilaDeDocumentosRetidos(
            documentos.Object,
            ComSituacao(SituacaoAssinatura.Suspensa),
            new RelogioFixo(),
            NullLogger<FilaDeDocumentosRetidos>.Instance);

        var vigente = await fila.ReceberAsync(Tenant, "s3://x", "fatura", CancellationToken.None);

        Assert.False(vigente);
        documentos.Verify(
            d => d.ReterAsync(
                It.Is<DocumentoRetidoEntity>(doc => doc.Pendente && doc.TenantId == Tenant),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>Liberar com assinatura ainda suspensa não processa nada: a barreira continua de pé.</summary>
    [Fact]
    public async Task Liberar_com_assinatura_suspensa_nao_processa()
    {
        var documentos = new Mock<IDocumentoRetidoRepository>();

        var fila = new FilaDeDocumentosRetidos(
            documentos.Object,
            ComSituacao(SituacaoAssinatura.Suspensa),
            new RelogioFixo(),
            NullLogger<FilaDeDocumentosRetidos>.Instance);

        var resultado = await fila.LiberarAsync(CancellationToken.None);

        Assert.Equal(0, resultado.Liberados);
        documentos.Verify(d => d.PendentesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Com a assinatura vigente, o que estava retido é marcado como processado.</summary>
    [Fact]
    public async Task Liberar_com_assinatura_vigente_processa_os_pendentes()
    {
        var pendentes = new[] { Documento(), Documento() };
        var documentos = new Mock<IDocumentoRetidoRepository>();

        documentos.Setup(d => d.PendentesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendentes);
        documentos.Setup(d => d.MarcarProcessadosAsync(
                It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendentes.Length);

        var fila = new FilaDeDocumentosRetidos(
            documentos.Object,
            ComSituacao(SituacaoAssinatura.Ativa),
            new RelogioFixo(),
            NullLogger<FilaDeDocumentosRetidos>.Instance);

        var resultado = await fila.LiberarAsync(CancellationToken.None);

        Assert.Equal(2, resultado.Liberados);
        documentos.Verify(
            d => d.MarcarProcessadosAsync(
                It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 2),
                Agora,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>Vigente, mas sem nada retido: não chama a marcação à toa.</summary>
    [Fact]
    public async Task Liberar_sem_pendentes_nao_marca_nada()
    {
        var documentos = new Mock<IDocumentoRetidoRepository>();
        documentos.Setup(d => d.PendentesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var fila = new FilaDeDocumentosRetidos(
            documentos.Object,
            ComSituacao(SituacaoAssinatura.Ativa),
            new RelogioFixo(),
            NullLogger<FilaDeDocumentosRetidos>.Instance);

        var resultado = await fila.LiberarAsync(CancellationToken.None);

        Assert.Equal(0, resultado.Liberados);
        documentos.Verify(
            d => d.MarcarProcessadosAsync(
                It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------------------------------------------------------------- disparo na regularização

    private sealed class ContextoAssumido : IContextoAssumivel
    {
        public Guid? UsuarioAssumido { get; private set; }

        public Guid? TenantAssumido { get; private set; }

        public void Assumir(Guid usuarioId, Guid tenantId)
        {
            UsuarioAssumido = usuarioId;
            TenantAssumido = tenantId;
        }
    }

    /// <summary>
    /// A regularização assume o tenant do titular e libera a fila. Assumir o contexto é o que faz
    /// a fila liberada ser a dele, mesmo quando o gatilho chegou sem token — pela notificação do
    /// provedor.
    /// </summary>
    [Fact]
    public async Task Regularizacao_assume_o_tenant_do_titular_e_libera_a_fila()
    {
        var usuario = Guid.CreateVersion7();
        var tenants = new Mock<IRotinaReadRepository>();
        tenants.Setup(t => t.TenantDoUsuarioAsync(usuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tenant);

        var documentos = new Mock<IDocumentoRetidoRepository>();
        documentos.Setup(d => d.PendentesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Documento()]);
        documentos.Setup(d => d.MarcarProcessadosAsync(
                It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var fila = new FilaDeDocumentosRetidos(
            documentos.Object,
            ComSituacao(SituacaoAssinatura.Ativa),
            new RelogioFixo(),
            NullLogger<FilaDeDocumentosRetidos>.Instance);

        var contexto = new ContextoAssumido();

        var handler = new LiberarFilaRetidaAoRegularizar(
            tenants.Object, contexto, fila, NullLogger<LiberarFilaRetidaAoRegularizar>.Instance);

        await handler.HandleAsync(
            new AssinaturaRegularizada(usuario, Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(usuario, contexto.UsuarioAssumido);
        Assert.Equal(Tenant, contexto.TenantAssumido);
        documentos.Verify(
            d => d.MarcarProcessadosAsync(
                It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Conta sem tenant próprio — excluída, ou a meio caminho da criação — não tem fila a liberar,
    /// e tratar isso como falha faria a contratação devolver erro depois de já ter cobrado.
    /// </summary>
    [Fact]
    public async Task Regularizacao_sem_tenant_proprio_nao_assume_nem_libera()
    {
        var usuario = Guid.CreateVersion7();
        var tenants = new Mock<IRotinaReadRepository>();
        tenants.Setup(t => t.TenantDoUsuarioAsync(usuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var documentos = new Mock<IDocumentoRetidoRepository>();

        var fila = new FilaDeDocumentosRetidos(
            documentos.Object,
            ComSituacao(SituacaoAssinatura.Ativa),
            new RelogioFixo(),
            NullLogger<FilaDeDocumentosRetidos>.Instance);

        var contexto = new ContextoAssumido();

        var handler = new LiberarFilaRetidaAoRegularizar(
            tenants.Object, contexto, fila, NullLogger<LiberarFilaRetidaAoRegularizar>.Instance);

        await handler.HandleAsync(
            new AssinaturaRegularizada(usuario, Guid.CreateVersion7()), CancellationToken.None);

        Assert.Null(contexto.TenantAssumido);
        documentos.Verify(d => d.PendentesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------------------------------------------------------------- reconciliação

    private static AssinaturaEntity Vigente(
        SituacaoAssinatura situacao,
        CanalPagamento canal = CanalPagamento.GooglePlay) => new()
    {
        Id = Guid.CreateVersion7(),
        UsuarioId = Guid.CreateVersion7(),
        PlanoId = Guid.CreateVersion7(),
        Situacao = situacao,
        Canal = canal,
        PeriodoFim = Agora.AddDays(10),
        ComprovanteReferencia = "GPA.1",
        CriadoEm = Agora.AddMonths(-1),
    };

    private static Mock<IAssinaturaWriteRepository> ComVigentes(params AssinaturaEntity[] vigentes)
    {
        var assinaturas = new Mock<IAssinaturaWriteRepository>();
        assinaturas.Setup(r => r.VigentesParaReconciliacaoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(vigentes);
        assinaturas.Setup(r => r.AtualizarSituacaoAsync(
                It.IsAny<AssinaturaEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return assinaturas;
    }

    private static RotinaDeReconciliacao Rotina(
        Mock<IAssinaturaWriteRepository> assinaturas, params IProvedorDePagamento[] provedores) =>
        new(assinaturas.Object, provedores, new RelogioFixo(), NullLogger<RotinaDeReconciliacao>.Instance);

    /// <summary>
    /// Notificação perdida: a conta ficou suspensa, mas o provedor confirma que está paga. A
    /// reconciliação reativa.
    /// </summary>
    [Fact]
    public async Task Reconciliacao_reativa_a_suspensa_que_o_provedor_confirma()
    {
        var assinaturas = ComVigentes(Vigente(SituacaoAssinatura.Suspensa));

        var provedor = new ProvedorFalso(
            CanalPagamento.GooglePlay, ConfirmacaoDeCompra.Aceita("GPA.1", Agora.AddMonths(1)));

        var resultado = await Rotina(assinaturas, provedor).ExecutarAsync(CancellationToken.None);

        Assert.Equal(1, resultado.Corrigidas);
        assinaturas.Verify(
            r => r.AtualizarSituacaoAsync(
                It.Is<AssinaturaEntity>(a => a.Situacao == SituacaoAssinatura.Ativa),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Cancelamento sem aviso: a conta seguia ativa, mas o provedor não confirma mais. A
    /// reconciliação suspende.
    /// </summary>
    [Fact]
    public async Task Reconciliacao_suspende_a_ativa_que_o_provedor_nao_confirma_mais()
    {
        var assinaturas = ComVigentes(Vigente(SituacaoAssinatura.Ativa));

        var provedor = new ProvedorFalso(
            CanalPagamento.GooglePlay, ConfirmacaoDeCompra.Recusada("cancelada"));

        var resultado = await Rotina(assinaturas, provedor).ExecutarAsync(CancellationToken.None);

        Assert.Equal(1, resultado.Corrigidas);
        assinaturas.Verify(
            r => r.AtualizarSituacaoAsync(
                It.Is<AssinaturaEntity>(a => a.Situacao == SituacaoAssinatura.Suspensa),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Sem divergência não há escrita: uma gravação por assinatura vigente a cada execução
    /// encheria a auditoria de linhas que não mudaram nada.
    /// </summary>
    [Fact]
    public async Task Reconciliacao_sem_divergencia_nao_grava()
    {
        var assinaturas = ComVigentes(Vigente(SituacaoAssinatura.Ativa));

        var provedor = new ProvedorFalso(
            CanalPagamento.GooglePlay,
            ConfirmacaoDeCompra.Aceita("GPA.1", Agora.AddDays(10)));

        var resultado = await Rotina(assinaturas, provedor).ExecutarAsync(CancellationToken.None);

        Assert.Equal(0, resultado.Corrigidas);
        Assert.Equal(1, resultado.Verificadas);
        assinaturas.Verify(
            r => r.AtualizarSituacaoAsync(It.IsAny<AssinaturaEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Provedor fora do ar não muda nada: derrubar assinaturas porque a API da loja caiu
    /// transformaria um incidente deles num incidente nosso, com clientes pagantes bloqueados.
    /// </summary>
    [Fact]
    public async Task Reconciliacao_com_provedor_indisponivel_nao_altera()
    {
        var assinaturas = ComVigentes(Vigente(SituacaoAssinatura.Ativa));

        var provedor = new ProvedorFalso(
            CanalPagamento.GooglePlay, ConfirmacaoDeCompra.Recusada("provedor_indisponivel"));

        var resultado = await Rotina(assinaturas, provedor).ExecutarAsync(CancellationToken.None);

        Assert.Equal(0, resultado.Corrigidas);
        Assert.Equal(1, resultado.Falhas);
        assinaturas.Verify(
            r => r.AtualizarSituacaoAsync(It.IsAny<AssinaturaEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Assinatura num canal sem provedor registrado conta como falha e fica intacta.</summary>
    [Fact]
    public async Task Reconciliacao_sem_provedor_para_o_canal_conta_falha()
    {
        var assinaturas = ComVigentes(Vigente(SituacaoAssinatura.Ativa, CanalPagamento.Asaas));

        var provedor = new ProvedorFalso(
            CanalPagamento.GooglePlay, ConfirmacaoDeCompra.Aceita("GPA.1", Agora.AddMonths(1)));

        var resultado = await Rotina(assinaturas, provedor).ExecutarAsync(CancellationToken.None);

        Assert.Equal(1, resultado.Falhas);
        Assert.Equal(0, resultado.Verificadas);
        assinaturas.Verify(
            r => r.AtualizarSituacaoAsync(It.IsAny<AssinaturaEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
