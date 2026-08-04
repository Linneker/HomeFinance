using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;
using Acme.Sistemas.Services.V1.Compartilhamento.Command.RevogarCompartilhamento;
using Acme.Sistemas.Services.V1.Compartilhamento.Query.ItensDoConsentimento;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

public sealed class CompartilhamentoTests
{
    private static readonly Guid Ana = Guid.CreateVersion7();
    private static readonly Guid TenantAna = Guid.CreateVersion7();
    private static readonly Guid Bruno = Guid.CreateVersion7();
    private static readonly Guid TenantBruno = Guid.CreateVersion7();
    private static readonly Guid Carla = Guid.CreateVersion7();

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);
    }

    private static Mock<ITenantWriteRepository> Repositorio(
        Guid? tenantDoDono = null, Guid? destinatario = null, bool jaVigente = false)
    {
        var tenants = new Mock<ITenantWriteRepository>();
        tenants.Setup(r => r.ObterTenantDoUsuarioAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantDoDono);
        tenants.Setup(r => r.ObterUsuarioPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(destinatario);
        tenants.Setup(r => r.ExisteCompartilhamentoVigenteAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jaVigente);
        return tenants;
    }

    private static CompartilharTenantCommand Comando(
        bool confirmado = true, bool scr = false, Guid? tenantSelecionado = null) =>
        new(Ana, tenantSelecionado ?? TenantAna, "bruno@exemplo.com.br", confirmado, scr);

    [Fact]
    public async Task Concessao_usa_o_tenant_do_autenticado_nunca_um_informado()
    {
        var tenants = Repositorio(TenantAna, Bruno);
        var handler = new CompartilharTenantCommandHandler(tenants.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(Comando(), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        // É o que impede quem só recebeu leitura de repassar o acesso a um terceiro.
        tenants.Verify(r => r.ObterTenantDoUsuarioAsync(Ana, It.IsAny<CancellationToken>()), Times.Once);
        tenants.Verify(
            r => r.ConcederAsync(
                It.Is<TenantCompartilhamentoEntity>(c =>
                    c.TenantId == TenantAna && c.UsuarioDestinatarioId == Bruno && !c.IncluiScr),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Scr_fica_de_fora_por_padrao_e_so_entra_marcado()
    {
        var tenants = Repositorio(TenantAna, Bruno);
        var handler = new CompartilharTenantCommandHandler(tenants.Object, new RelogioFixo());

        await handler.HandleAsync(Comando(scr: true), CancellationToken.None);

        tenants.Verify(
            r => r.ConcederAsync(
                It.Is<TenantCompartilhamentoEntity>(c => c.IncluiScr),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Consentimento_itemizado_traz_o_sensivel_desmarcado_e_o_resto_marcado()
    {
        var resultado = await new ItensDoConsentimentoQueryHandler()
            .HandleAsync(new ItensDoConsentimentoQuery(Ana), CancellationToken.None);

        // A tela é montada a partir daqui. Se o sensível nascesse marcado, o opt-in viraria
        // decoração: o dono confirmaria sem nunca ter decidido incluí-lo.
        var scr = resultado.Itens.Single(item => item.Chave == CatalogoDeCompartilhamento.ChaveScr);

        Assert.True(scr.Sensivel);
        Assert.False(scr.MarcadoPorPadrao);

        Assert.Contains(resultado.Itens, item => !item.Sensivel);
        Assert.All(
            resultado.Itens.Where(item => !item.Sensivel),
            item => Assert.True(item.MarcadoPorPadrao));
    }

    [Fact]
    public async Task Todo_item_sensivel_tem_onde_gravar_o_opt_in()
    {
        var resultado = await new ItensDoConsentimentoQueryHandler()
            .HandleAsync(new ItensDoConsentimentoQuery(Ana), CancellationToken.None);

        // Só existe uma coluna de opt-in — IncluiScr. Um segundo item sensível acrescentado
        // ao catálogo apareceria desmarcado na tela e seria compartilhado assim mesmo, porque
        // não haveria onde registrar a escolha do dono. Este teste quebra antes disso chegar
        // ao usuário.
        var sensiveis = resultado.Itens.Where(item => item.Sensivel).Select(item => item.Chave);

        Assert.Equal([CatalogoDeCompartilhamento.ChaveScr], sensiveis);
    }

    [Fact]
    public async Task Sem_confirmacao_do_consentimento_nada_e_concedido()
    {
        var tenants = Repositorio(TenantAna, Bruno);
        var handler = new CompartilharTenantCommandHandler(tenants.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(Comando(confirmado: false), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            CompartilharTenantCommandValidation.CodigoConsentimentoNaoConfirmado,
            resultado.Erros[0].Codigo);
        tenants.Verify(
            r => r.ConcederAsync(It.IsAny<TenantCompartilhamentoEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Compartilhar_consigo_mesmo_e_recusado()
    {
        var tenants = Repositorio(TenantAna, Ana);
        var handler = new CompartilharTenantCommandHandler(tenants.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(Comando(), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            CompartilharTenantCommandHandler.CodigoCompartilhamentoConsigoMesmo,
            resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Concessao_duplicada_e_recusada()
    {
        var tenants = Repositorio(TenantAna, Bruno, jaVigente: true);
        var handler = new CompartilharTenantCommandHandler(tenants.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(Comando(), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(CompartilharTenantCommandHandler.CodigoJaCompartilhado, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Destinatario_inexistente_e_recusado()
    {
        var tenants = Repositorio(TenantAna, destinatario: null);
        var handler = new CompartilharTenantCommandHandler(tenants.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(Comando(), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            CompartilharTenantCommandHandler.CodigoDestinatarioNaoEncontrado,
            resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Compartilhamento_e_assimetrico_e_nao_concede_o_caminho_inverso()
    {
        var tenants = Repositorio(TenantAna, Bruno);
        var handler = new CompartilharTenantCommandHandler(tenants.Object, new RelogioFixo());

        await handler.HandleAsync(Comando(), CancellationToken.None);

        // Ana compartilhar com Bruno não cria acesso da Ana ao tenant do Bruno. Uma única
        // concessão, e no sentido declarado.
        tenants.Verify(
            r => r.ConcederAsync(
                It.Is<TenantCompartilhamentoEntity>(c =>
                    c.TenantId == TenantBruno || c.UsuarioDestinatarioId == Ana),
                It.IsAny<CancellationToken>()),
            Times.Never);
        tenants.Verify(
            r => r.ConcederAsync(It.IsAny<TenantCompartilhamentoEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Quem_so_tem_leitura_nao_repassa_o_acesso()
    {
        // Bruno recebeu o tenant da Ana e está com ele aberto. Pede compartilhamento com Carla.
        var tenants = Repositorio(TenantBruno, Carla);
        var handler = new CompartilharTenantCommandHandler(tenants.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new CompartilharTenantCommand(Bruno, TenantAna, "carla@exemplo.com.br", true, false),
            CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            CompartilharTenantCommandHandler.CodigoRepasseNaoPermitido,
            resultado.Erros[0].Codigo);

        // Nem repassa o tenant da Ana, nem compartilha o do Bruno por engano — que é o que
        // aconteceria se a recusa não existisse, já que a concessão sai do tenant do autenticado.
        tenants.Verify(
            r => r.ConcederAsync(It.IsAny<TenantCompartilhamentoEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
        tenants.Verify(
            r => r.ObterUsuarioPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Revogacao_usa_o_tenant_do_autenticado()
    {
        var tenants = Repositorio(TenantAna);
        tenants.Setup(r => r.RevogarAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new RevogarCompartilhamentoCommandHandler(tenants.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new RevogarCompartilhamentoCommand(Ana, TenantAna, Bruno), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        tenants.Verify(
            r => r.RevogarAsync(TenantAna, Bruno, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Revogar_com_tenant_de_terceiro_aberto_e_recusado()
    {
        var tenants = Repositorio(TenantBruno);
        var handler = new RevogarCompartilhamentoCommandHandler(tenants.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new RevogarCompartilhamentoCommand(Bruno, TenantAna, Carla), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            RevogarCompartilhamentoCommandHandler.CodigoRevogacaoForaDoProprioTenant,
            resultado.Erros[0].Codigo);
        tenants.Verify(
            r => r.RevogarAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Revogar_o_que_nao_existe_e_recusado()
    {
        var tenants = Repositorio(TenantAna);
        tenants.Setup(r => r.RevogarAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new RevogarCompartilhamentoCommandHandler(tenants.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new RevogarCompartilhamentoCommand(Ana, TenantAna, Bruno), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            RevogarCompartilhamentoCommandHandler.CodigoCompartilhamentoInexistente,
            resultado.Erros[0].Codigo);
    }
}
