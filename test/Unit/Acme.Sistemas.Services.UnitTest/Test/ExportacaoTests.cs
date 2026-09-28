using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.FeatureFlags;
using Acme.Sistemas.Infrastructure.Messaging;
using Acme.Sistemas.Services.V1.Exportacao;
using Acme.Sistemas.Services.V1.Exportacao.Command.DesabilitarExportacao;
using Acme.Sistemas.Services.V1.Exportacao.Command.HabilitarExportacao;
using Acme.Sistemas.Services.V1.Exportacao.Command.SolicitarExportacao;
using Acme.Sistemas.Services.V1.Exportacao.Formatadores;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

/// <summary>
/// A lógica da exportação isolada: as travas que liberam ou recusam a solicitação, a exigência
/// de motivo na habilitação e a prevalência sobre a assinatura. O isolamento no banco e a
/// exclusividade administrativa do endpoint são provados pela integração.
/// </summary>
public sealed class ExportacaoTests
{
    private static readonly Guid Tenant = Guid.CreateVersion7();
    private static readonly Guid Usuario = Guid.CreateVersion7();
    private static readonly DateTimeOffset Agora = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Agora;
    }

    private static IFeatureFlagService ComFlag(bool ligada)
    {
        var flags = new Mock<IFeatureFlagService>();
        flags.Setup(f => f.Habilitada(FeatureFlags.ExportacaoDados)).Returns(ligada);
        return flags.Object;
    }

    private static IResolvedorDeFormato Resolvedor() =>
        new ResolvedorDeFormato([new FormatadorCsv(), new FormatadorJson(), new FormatadorTexto()]);

    private static SolicitarExportacaoCommandHandler Handler(
        bool flagLigada,
        bool habilitada,
        Mock<IExportacaoSolicitacaoWriteRepository> solicitacoes,
        Mock<IFilaExportacao> fila,
        Mock<IAuditoriaRepository> auditoria)
    {
        var habilitacoes = new Mock<IExportacaoHabilitacaoWriteRepository>();
        habilitacoes.Setup(h => h.HabilitadaAsync(Usuario, It.IsAny<CancellationToken>()))
            .ReturnsAsync(habilitada);

        return new SolicitarExportacaoCommandHandler(
            ComFlag(flagLigada),
            habilitacoes.Object,
            Resolvedor(),
            solicitacoes.Object,
            fila.Object,
            auditoria.Object,
            new RelogioFixo());
    }

    // ---------------------------------------------------------------- solicitar

    /// <summary>Sem habilitação para a conta, a solicitação é recusada e nada é enfileirado.</summary>
    [Fact]
    public async Task Solicitar_sem_habilitacao_e_recusado()
    {
        var solicitacoes = new Mock<IExportacaoSolicitacaoWriteRepository>();
        var fila = new Mock<IFilaExportacao>();

        var resultado = await Handler(
                flagLigada: true, habilitada: false, solicitacoes, fila, new Mock<IAuditoriaRepository>())
            .HandleAsync(
                new SolicitarExportacaoCommand(Tenant, Usuario, SomenteLeitura: false, FormatoExportacao.Csv),
                CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(SolicitarExportacaoCommandHandler.CodigoNaoHabilitada, resultado.Erros[0].Codigo);
        solicitacoes.Verify(s => s.CriarAsync(It.IsAny<Domain.Entities.Exportacao.ExportacaoSolicitacaoEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        fila.Verify(f => f.PublicarAsync(It.IsAny<MensagemExportacao>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Destinatário de compartilhamento (somente leitura) não exporta o tenant recebido.</summary>
    [Fact]
    public async Task Solicitar_de_tenant_compartilhado_e_recusado()
    {
        var solicitacoes = new Mock<IExportacaoSolicitacaoWriteRepository>();

        var resultado = await Handler(
                flagLigada: true, habilitada: true, solicitacoes, new Mock<IFilaExportacao>(), new Mock<IAuditoriaRepository>())
            .HandleAsync(
                new SolicitarExportacaoCommand(Tenant, Usuario, SomenteLeitura: true, FormatoExportacao.Csv),
                CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(SolicitarExportacaoCommandHandler.CodigoTenantCompartilhado, resultado.Erros[0].Codigo);
        solicitacoes.Verify(s => s.CriarAsync(It.IsAny<Domain.Entities.Exportacao.ExportacaoSolicitacaoEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Formato não oferecido é recusado, e a resposta lista os formatos disponíveis.</summary>
    [Fact]
    public async Task Solicitar_com_formato_nao_suportado_e_recusado()
    {
        var resultado = await Handler(
                flagLigada: true, habilitada: true, new Mock<IExportacaoSolicitacaoWriteRepository>(), new Mock<IFilaExportacao>(), new Mock<IAuditoriaRepository>())
            .HandleAsync(
                new SolicitarExportacaoCommand(Tenant, Usuario, SomenteLeitura: false, (FormatoExportacao)999),
                CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(SolicitarExportacaoCommandHandler.CodigoFormatoNaoSuportado, resultado.Erros[0].Codigo);
        Assert.NotEmpty(resultado.FormatosDisponiveis);
    }

    /// <summary>
    /// A habilitação prevalece sobre a assinatura: nada no fluxo consulta a situação da assinatura,
    /// então a solicitação é aceita — e enfileirada e auditada — mesmo com a conta suspensa.
    /// </summary>
    [Fact]
    public async Task Solicitar_habilitado_prevalece_sobre_assinatura()
    {
        var solicitacoes = new Mock<IExportacaoSolicitacaoWriteRepository>();
        var fila = new Mock<IFilaExportacao>();
        var auditoria = new Mock<IAuditoriaRepository>();

        var resultado = await Handler(flagLigada: true, habilitada: true, solicitacoes, fila, auditoria)
            .HandleAsync(
                new SolicitarExportacaoCommand(Tenant, Usuario, SomenteLeitura: false, FormatoExportacao.Json),
                CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.NotNull(resultado.SolicitacaoId);
        solicitacoes.Verify(s => s.CriarAsync(It.IsAny<Domain.Entities.Exportacao.ExportacaoSolicitacaoEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        fila.Verify(f => f.PublicarAsync(It.IsAny<MensagemExportacao>(), It.IsAny<CancellationToken>()), Times.Once);
        auditoria.Verify(a => a.RegistrarAsync(It.IsAny<Domain.Entities.Auditoria.RegistroAuditoriaEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------------------------------------------------------------- habilitar

    /// <summary>Habilitar sem motivo é recusado: a razão registrada é o que torna a concessão auditável.</summary>
    [Fact]
    public async Task Habilitar_sem_motivo_e_recusado()
    {
        var habilitacoes = new Mock<IExportacaoHabilitacaoWriteRepository>();

        var handler = new HabilitarExportacaoCommandHandler(
            habilitacoes.Object,
            new Mock<IAuditoriaRepository>().Object,
            new Mock<IAvisoTitular>().Object,
            new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new HabilitarExportacaoCommand(Usuario, "   ", AutorAdminId: Guid.CreateVersion7()),
            CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(HabilitarExportacaoCommandValidation.CodigoMotivoObrigatorio, resultado.Erros[0].Codigo);
        habilitacoes.Verify(
            h => h.DefinirAsync(It.IsAny<Domain.Entities.Exportacao.ExportacaoHabilitacaoEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Habilitar avisa o titular e registra na auditoria; a gravação marca "habilitada".</summary>
    [Fact]
    public async Task Habilitar_grava_avisa_e_audita()
    {
        var habilitacoes = new Mock<IExportacaoHabilitacaoWriteRepository>();
        var auditoria = new Mock<IAuditoriaRepository>();
        var avisos = new Mock<IAvisoTitular>();

        var handler = new HabilitarExportacaoCommandHandler(
            habilitacoes.Object, auditoria.Object, avisos.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new HabilitarExportacaoCommand(Usuario, "Requisição de acesso do titular", Guid.CreateVersion7()),
            CancellationToken.None);

        Assert.True(resultado.Sucesso);
        habilitacoes.Verify(
            h => h.DefinirAsync(It.Is<Domain.Entities.Exportacao.ExportacaoHabilitacaoEntity>(e => e.Habilitada && e.UsuarioId == Usuario), It.IsAny<CancellationToken>()),
            Times.Once);
        avisos.Verify(a => a.AvisarAsync(Usuario, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        auditoria.Verify(a => a.RegistrarAsync(It.IsAny<Domain.Entities.Auditoria.RegistroAuditoriaEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Desabilitar cancela as solicitações pendentes da conta.</summary>
    [Fact]
    public async Task Desabilitar_cancela_pendentes()
    {
        var habilitacoes = new Mock<IExportacaoHabilitacaoWriteRepository>();
        var solicitacoes = new Mock<IExportacaoSolicitacaoWriteRepository>();
        solicitacoes.Setup(s => s.CancelarPendentesAsync(Usuario, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var handler = new DesabilitarExportacaoCommandHandler(
            habilitacoes.Object, solicitacoes.Object, new Mock<IAuditoriaRepository>().Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new DesabilitarExportacaoCommand(Usuario, "Encerramento a pedido", Guid.CreateVersion7()),
            CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(2, resultado.PendentesCanceladas);
        habilitacoes.Verify(
            h => h.DefinirAsync(It.Is<Domain.Entities.Exportacao.ExportacaoHabilitacaoEntity>(e => !e.Habilitada), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
