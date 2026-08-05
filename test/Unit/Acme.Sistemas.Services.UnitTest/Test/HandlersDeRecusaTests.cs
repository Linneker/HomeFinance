using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Entities.Classificacao;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Services.V1.Classificacao.Command.AlterarRegra;
using Acme.Sistemas.Services.V1.Classificacao.Command.ClassificarLancamento;
using Acme.Sistemas.Services.V1.Classificacao.Command.RemoverRegra;
using Acme.Sistemas.Services.V1.Classificacao.Query.Regras;
using Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;
using Acme.Sistemas.Services.V1.Compartilhamento.Command.RevogarCompartilhamento;
using Acme.Sistemas.Services.V1.Compartilhamento.Query.TenantsDisponiveis;
using Acme.Sistemas.Services.V1.Identidade.Query.ConsentimentoVigente;
using Acme.Sistemas.Services.V1.Lancamentos.Command.AlterarLancamento;
using Acme.Sistemas.Services.V1.Lancamentos.Command.CriarCategoria;
using Acme.Sistemas.Services.V1.Lancamentos.Command.CriarRecorrencia;
using Acme.Sistemas.Services.V1.Lancamentos.Command.EncerrarRecorrencia;
using Acme.Sistemas.Services.V1.Lancamentos.Command.RemoverLancamento;
using Acme.Sistemas.Services.V1.Lancamentos.Query.Categorias;
using Acme.Sistemas.Services.V1.Lancamentos.Query.Lancamentos;
using Acme.Sistemas.Services.V1.Assinaturas;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

/// <summary>
/// Consultas simples e os caminhos de recusa dos handlers.
/// </summary>
/// <remarks>
/// Cada recusa aqui é um estado em que o sistema tem de dizer "não" com o motivo certo. Sem
/// teste, o caminho feliz continua passando e a recusa errada só aparece para o cliente — que
/// vê "não encontrado" onde deveria ver "não permitido", ou o contrário.
/// </remarks>
public sealed class HandlersDeRecusaTests
{
    /// <summary>Assinatura ativa com cota folgada: a cota tem grupo de teste próprio.</summary>
    private static IAssinaturaService ComCota(int total = 3, int usada = 0)
    {
        var assinaturas = new Mock<IAssinaturaService>();

        assinaturas.Setup(a => a.SituacaoAtualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SituacaoDaConta(
                Domain.Enums.SituacaoAssinatura.Ativa, "casal", total, usada));

        return assinaturas.Object;
    }

    private static readonly Guid Tenant = Guid.CreateVersion7();
    private static readonly Guid Usuario = Guid.CreateVersion7();
    private static readonly Guid Alvo = Guid.CreateVersion7();
    private static readonly Guid Categoria = Guid.CreateVersion7();
    private static readonly DateOnly Agosto = new(2026, 8, 1);

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);
    }

    private static Mock<ICategoriaWriteRepository> Categorias(bool existe)
    {
        var categorias = new Mock<ICategoriaWriteRepository>();

        categorias.Setup(r => r.ExisteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existe);
        categorias.Setup(r => r.ExisteNomeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        return categorias;
    }

    private static LancamentoEntity Lancamento(string descricao = "Mercado") => new()
    {
        Id = Alvo,
        TenantId = Tenant,
        Descricao = descricao,
        Valor = 100m,
        DataCompetencia = Agosto,
        DataCaixa = Agosto,
        CriadoEm = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
    };

    // ---------------------------------------------------------------- categorias

    [Fact]
    public async Task Categoria_criada_devolve_o_identificador()
    {
        CategoriaEntity? gravada = null;
        var categorias = Categorias(existe: true);

        categorias.Setup(r => r.CriarAsync(
                It.IsAny<CategoriaEntity>(), It.IsAny<CancellationToken>()))
            .Callback<CategoriaEntity, CancellationToken>((categoria, _) => gravada = categoria)
            .Returns(Task.CompletedTask);

        var resultado = await new CriarCategoriaCommandHandler(categorias.Object, new RelogioFixo())
            .HandleAsync(new CriarCategoriaCommand(Tenant, "  Mercado  "), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.NotNull(gravada);
        Assert.Equal("Mercado", gravada.Nome);
        Assert.Equal(Tenant, gravada.TenantId);
    }

    /// <summary>
    /// Duas categorias iguais quebrariam o resumo em duas linhas com o mesmo rótulo, e a pessoa
    /// não teria como saber em qual delas lançar.
    /// </summary>
    [Fact]
    public async Task Categoria_com_nome_repetido_e_recusada()
    {
        var categorias = Categorias(existe: true);
        categorias.Setup(r => r.ExisteNomeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var resultado = await new CriarCategoriaCommandHandler(categorias.Object, new RelogioFixo())
            .HandleAsync(new CriarCategoriaCommand(Tenant, "Mercado"), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(CriarCategoriaCommandHandler.CodigoNomeEmUso, resultado.Erros[0].Codigo);

        categorias.Verify(
            r => r.CriarAsync(It.IsAny<CategoriaEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Categoria_sem_nome_e_recusada_antes_de_consultar_o_banco()
    {
        var categorias = Categorias(existe: true);

        var resultado = await new CriarCategoriaCommandHandler(categorias.Object, new RelogioFixo())
            .HandleAsync(new CriarCategoriaCommand(Tenant, "  "), CancellationToken.None);

        Assert.False(resultado.Sucesso);

        categorias.Verify(
            r => r.ExisteNomeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consulta_de_categorias_devolve_a_lista_do_tenant()
    {
        var categorias = new Mock<ICategoriaReadRepository>();
        categorias.Setup(r => r.ListarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new CategoriaEntity { Id = Categoria, TenantId = Tenant, Nome = "Mercado" },
            ]);

        var resultado = await new CategoriasQueryHandler(categorias.Object)
            .HandleAsync(new CategoriasQuery(Tenant), CancellationToken.None);

        var listada = Assert.Single(resultado.Categorias);

        Assert.Equal("Mercado", listada.Nome);
    }

    [Fact]
    public async Task Consulta_de_categorias_sem_tenant_devolve_vazio()
    {
        var categorias = new Mock<ICategoriaReadRepository>();

        var resultado = await new CategoriasQueryHandler(categorias.Object)
            .HandleAsync(new CategoriasQuery(Guid.Empty), CancellationToken.None);

        Assert.Empty(resultado.Categorias);
        categorias.Verify(r => r.ListarAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------------------------------------------------------------- extrato simples

    [Fact]
    public async Task Consulta_de_lancamentos_devolve_o_periodo_na_visao_pedida()
    {
        var lancamentos = new Mock<ILancamentoReadRepository>();
        FiltroDeLancamento? usado = null;

        lancamentos.Setup(r => r.ListarAsync(
                It.IsAny<FiltroDeLancamento>(), It.IsAny<CancellationToken>()))
            .Callback<FiltroDeLancamento, CancellationToken>((filtro, _) => usado = filtro)
            .ReturnsAsync([Lancamento()]);

        var resultado = await new LancamentosQueryHandler(lancamentos.Object).HandleAsync(
            new LancamentosQuery(Tenant, Agosto, new DateOnly(2026, 8, 31), PorCaixa: true, Categoria),
            CancellationToken.None);

        Assert.Single(resultado.Lancamentos);
        Assert.True(resultado.VisaoPorCaixa);
        Assert.Equal(100m, resultado.Total);

        Assert.NotNull(usado);
        Assert.True(usado.PorCaixa);
        Assert.Equal(Categoria, usado.CategoriaId);
    }

    [Fact]
    public async Task Consulta_de_lancamentos_sem_periodo_e_recusada_sem_ir_ao_banco()
    {
        var lancamentos = new Mock<ILancamentoReadRepository>();

        var resultado = await new LancamentosQueryHandler(lancamentos.Object)
            .HandleAsync(new LancamentosQuery(Tenant, null, null), CancellationToken.None);

        Assert.Empty(resultado.Lancamentos);
        Assert.Single(resultado.Erros);

        lancamentos.Verify(
            r => r.ListarAsync(It.IsAny<FiltroDeLancamento>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------------------------------------------------------------- regras

    [Fact]
    public async Task Consulta_de_regras_devolve_a_lista_na_ordem_do_repositorio()
    {
        var regras = new Mock<IRegraClassificacaoReadRepository>();
        regras.Setup(r => r.ListarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new RegraClassificacaoEntity
                {
                    Id = Alvo,
                    TenantId = Tenant,
                    Padrao = "IFOOD",
                    CategoriaId = Categoria,
                    CriadoEm = DateTimeOffset.UtcNow,
                },
            ]);

        var resultado = await new RegrasQueryHandler(regras.Object)
            .HandleAsync(new RegrasQuery(Tenant), CancellationToken.None);

        var regra = Assert.Single(resultado.Regras);

        Assert.Equal("IFOOD", regra.Padrao);
    }

    [Fact]
    public async Task Consulta_de_regras_sem_tenant_devolve_vazio()
    {
        var regras = new Mock<IRegraClassificacaoReadRepository>();

        var resultado = await new RegrasQueryHandler(regras.Object)
            .HandleAsync(new RegrasQuery(Guid.Empty), CancellationToken.None);

        Assert.Empty(resultado.Regras);
        regras.Verify(r => r.ListarAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Alteracao_de_regra_inexistente_e_recusada()
    {
        var regras = new Mock<IRegraClassificacaoWriteRepository>();
        regras.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RegraClassificacaoEntity?)null);

        var resultado = await new AlterarRegraCommandHandler(
                regras.Object, Categorias(existe: true).Object)
            .HandleAsync(
                new AlterarRegraCommand(Tenant, Alvo, "IFOOD", Categoria), CancellationToken.None);

        Assert.Equal(
            AlterarRegraCommandHandler.CodigoRegraNaoEncontrada, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Alteracao_de_regra_para_categoria_inexistente_e_recusada()
    {
        var regras = new Mock<IRegraClassificacaoWriteRepository>();
        regras.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegraClassificacaoEntity
            {
                Id = Alvo,
                TenantId = Tenant,
                Padrao = "IFOOD",
                CategoriaId = Categoria,
                CriadoEm = DateTimeOffset.UtcNow,
            });

        var resultado = await new AlterarRegraCommandHandler(
                regras.Object, Categorias(existe: false).Object)
            .HandleAsync(
                new AlterarRegraCommand(Tenant, Alvo, "IFOOD", Categoria), CancellationToken.None);

        Assert.Equal(
            AlterarRegraCommandHandler.CodigoCategoriaInexistente, resultado.Erros[0].Codigo);
    }

    /// <summary>
    /// A regra sumiu entre a leitura e a gravação — outra sessão a removeu. A recusa tem de ser
    /// "não encontrada", e não um sucesso silencioso que não alterou nada.
    /// </summary>
    [Fact]
    public async Task Alteracao_de_regra_que_sumiu_ao_gravar_e_recusada()
    {
        var regras = new Mock<IRegraClassificacaoWriteRepository>();
        regras.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegraClassificacaoEntity
            {
                Id = Alvo,
                TenantId = Tenant,
                Padrao = "IFOOD",
                CategoriaId = Categoria,
                CriadoEm = DateTimeOffset.UtcNow,
            });
        regras.Setup(r => r.AlterarAsync(
                It.IsAny<RegraClassificacaoEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultado = await new AlterarRegraCommandHandler(
                regras.Object, Categorias(existe: true).Object)
            .HandleAsync(
                new AlterarRegraCommand(Tenant, Alvo, "IFOOD", Categoria), CancellationToken.None);

        Assert.Equal(
            AlterarRegraCommandHandler.CodigoRegraNaoEncontrada, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Remocao_de_regra_inexistente_e_recusada()
    {
        var regras = new Mock<IRegraClassificacaoWriteRepository>();
        regras.Setup(r => r.RemoverAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultado = await new RemoverRegraCommandHandler(regras.Object)
            .HandleAsync(new RemoverRegraCommand(Tenant, Alvo), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            RemoverRegraCommandHandler.CodigoRegraNaoEncontrada, resultado.Erros[0].Codigo);
    }

    // ---------------------------------------------------------------- classificação

    [Fact]
    public async Task Classificar_lancamento_inexistente_e_recusado()
    {
        var lancamentos = new Mock<ILancamentoWriteRepository>();
        lancamentos.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LancamentoEntity?)null);

        var resultado = await new ClassificarLancamentoCommandHandler(
                lancamentos.Object,
                Categorias(existe: true).Object,
                new Mock<IRegraClassificacaoWriteRepository>().Object,
                new RelogioFixo())
            .HandleAsync(
                new ClassificarLancamentoCommand(Tenant, Alvo, Categoria), CancellationToken.None);

        Assert.Equal(
            ClassificarLancamentoCommandHandler.CodigoLancamentoNaoEncontrado,
            resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Classificar_para_categoria_inexistente_e_recusado()
    {
        var lancamentos = new Mock<ILancamentoWriteRepository>();
        lancamentos.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Lancamento());

        var resultado = await new ClassificarLancamentoCommandHandler(
                lancamentos.Object,
                Categorias(existe: false).Object,
                new Mock<IRegraClassificacaoWriteRepository>().Object,
                new RelogioFixo())
            .HandleAsync(
                new ClassificarLancamentoCommand(Tenant, Alvo, Categoria), CancellationToken.None);

        Assert.Equal(
            ClassificarLancamentoCommandHandler.CodigoCategoriaInexistente,
            resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Classificacao_de_lancamento_que_sumiu_ao_gravar_e_recusada()
    {
        var lancamentos = new Mock<ILancamentoWriteRepository>();
        lancamentos.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Lancamento());
        lancamentos.Setup(r => r.AlterarAsync(
                It.IsAny<LancamentoEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultado = await new ClassificarLancamentoCommandHandler(
                lancamentos.Object,
                Categorias(existe: true).Object,
                new Mock<IRegraClassificacaoWriteRepository>().Object,
                new RelogioFixo())
            .HandleAsync(
                new ClassificarLancamentoCommand(Tenant, Alvo, Categoria), CancellationToken.None);

        Assert.Equal(
            ClassificarLancamentoCommandHandler.CodigoLancamentoNaoEncontrado,
            resultado.Erros[0].Codigo);
    }

    // ---------------------------------------------------------------- lançamentos

    [Fact]
    public async Task Alteracao_de_lancamento_inexistente_e_recusada()
    {
        var lancamentos = new Mock<ILancamentoWriteRepository>();
        lancamentos.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LancamentoEntity?)null);

        var resultado = await new AlterarLancamentoCommandHandler(
                lancamentos.Object, Categorias(existe: true).Object, new RelogioFixo())
            .HandleAsync(
                new AlterarLancamentoCommand(Tenant, Alvo, "Mercado", 10m, Agosto, Agosto, null),
                CancellationToken.None);

        Assert.Equal(
            AlterarLancamentoCommandHandler.CodigoLancamentoNaoEncontrado,
            resultado.Erros[0].Codigo);
    }

    /// <summary>
    /// Editar uma ocorrência ainda prevista seria trabalho perdido: a próxima materialização da
    /// recorrência a regeraria por cima.
    /// </summary>
    [Fact]
    public async Task Alteracao_de_ocorrencia_prevista_e_recusada()
    {
        var previsto = Lancamento();
        previsto.Previsto = true;

        var lancamentos = new Mock<ILancamentoWriteRepository>();
        lancamentos.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(previsto);

        var resultado = await new AlterarLancamentoCommandHandler(
                lancamentos.Object, Categorias(existe: true).Object, new RelogioFixo())
            .HandleAsync(
                new AlterarLancamentoCommand(Tenant, Alvo, "Mercado", 10m, Agosto, Agosto, null),
                CancellationToken.None);

        Assert.Equal(
            AlterarLancamentoCommandHandler.CodigoOcorrenciaPrevista, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Alteracao_para_categoria_inexistente_e_recusada()
    {
        var lancamentos = new Mock<ILancamentoWriteRepository>();
        lancamentos.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Lancamento());

        var resultado = await new AlterarLancamentoCommandHandler(
                lancamentos.Object, Categorias(existe: false).Object, new RelogioFixo())
            .HandleAsync(
                new AlterarLancamentoCommand(Tenant, Alvo, "Mercado", 10m, Agosto, Agosto, Categoria),
                CancellationToken.None);

        Assert.Equal(
            AlterarLancamentoCommandHandler.CodigoCategoriaInexistente, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Alteracao_de_lancamento_que_sumiu_ao_gravar_e_recusada()
    {
        var lancamentos = new Mock<ILancamentoWriteRepository>();
        lancamentos.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Lancamento());
        lancamentos.Setup(r => r.AlterarAsync(
                It.IsAny<LancamentoEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultado = await new AlterarLancamentoCommandHandler(
                lancamentos.Object, Categorias(existe: true).Object, new RelogioFixo())
            .HandleAsync(
                new AlterarLancamentoCommand(Tenant, Alvo, "Mercado", 10m, Agosto, Agosto, null),
                CancellationToken.None);

        Assert.Equal(
            AlterarLancamentoCommandHandler.CodigoLancamentoNaoEncontrado,
            resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Remocao_de_lancamento_inexistente_e_recusada()
    {
        var lancamentos = new Mock<ILancamentoWriteRepository>();
        lancamentos.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LancamentoEntity?)null);

        var resultado = await new RemoverLancamentoCommandHandler(lancamentos.Object)
            .HandleAsync(new RemoverLancamentoCommand(Tenant, Alvo), CancellationToken.None);

        Assert.Equal(
            RemoverLancamentoCommandHandler.CodigoLancamentoNaoEncontrado,
            resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Remocao_que_nao_apaga_linha_alguma_e_recusada()
    {
        var lancamentos = new Mock<ILancamentoWriteRepository>();
        lancamentos.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Lancamento());
        lancamentos.Setup(r => r.RemoverAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultado = await new RemoverLancamentoCommandHandler(lancamentos.Object)
            .HandleAsync(new RemoverLancamentoCommand(Tenant, Alvo), CancellationToken.None);

        Assert.False(resultado.Sucesso);
    }

    // ---------------------------------------------------------------- recorrência

    [Fact]
    public async Task Recorrencia_em_categoria_inexistente_e_recusada()
    {
        var resultado = await new CriarRecorrenciaCommandHandler(
                new Mock<IRecorrenciaWriteRepository>().Object,
                Categorias(existe: false).Object,
                new RelogioFixo())
            .HandleAsync(
                new CriarRecorrenciaCommand(
                    Tenant, Usuario, "Aluguel", 2000m, 10, Categoria, Agosto),
                CancellationToken.None);

        Assert.Equal(
            CriarRecorrenciaCommandHandler.CodigoCategoriaInexistente, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Encerramento_de_recorrencia_inexistente_e_recusado()
    {
        var recorrencias = new Mock<IRecorrenciaWriteRepository>();
        recorrencias.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecorrenciaEntity?)null);

        var resultado = await new EncerrarRecorrenciaCommandHandler(
                recorrencias.Object, new RelogioFixo())
            .HandleAsync(new EncerrarRecorrenciaCommand(Tenant, Alvo), CancellationToken.None);

        Assert.Equal(
            EncerrarRecorrenciaCommandHandler.CodigoRecorrenciaNaoEncontrada,
            resultado.Erros[0].Codigo);
    }

    // ---------------------------------------------------------------- compartilhamento

    [Fact]
    public async Task Compartilhar_sem_tenant_do_titular_e_recusado()
    {
        var tenants = new Mock<ITenantWriteRepository>();
        tenants.Setup(r => r.ObterTenantDoUsuarioAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var resultado = await new CompartilharTenantCommandHandler(
                tenants.Object, ComCota(), new RelogioFixo())
            .HandleAsync(
                new CompartilharTenantCommand(Usuario, null, "bruno@teste.local", true, false),
                CancellationToken.None);

        Assert.Equal(
            CompartilharTenantCommandHandler.CodigoTenantNaoEncontrado, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Revogar_sem_tenant_do_titular_e_recusado()
    {
        var tenants = new Mock<ITenantWriteRepository>();
        tenants.Setup(r => r.ObterTenantDoUsuarioAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var resultado = await new RevogarCompartilhamentoCommandHandler(tenants.Object, new RelogioFixo())
            .HandleAsync(
                new RevogarCompartilhamentoCommand(Usuario, null, Alvo), CancellationToken.None);

        Assert.Equal(
            RevogarCompartilhamentoCommandHandler.CodigoTenantNaoEncontrado,
            resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Consulta_de_tenants_devolve_proprio_e_compartilhados()
    {
        var tenants = new Mock<ITenantReadRepository>();
        tenants.Setup(r => r.ObterDisponiveisAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TenantDisponivel(Tenant, true, false),
                new TenantDisponivel(Alvo, false, true),
            ]);

        var resultado = await new TenantsDisponiveisQueryHandler(tenants.Object)
            .HandleAsync(new TenantsDisponiveisQuery(Usuario), CancellationToken.None);

        Assert.Equal(2, resultado.Tenants.Count);
        Assert.Contains(resultado.Tenants, t => t.EhProprio && !t.SomenteLeitura);
        Assert.Contains(resultado.Tenants, t => !t.EhProprio && t.SomenteLeitura);
    }

    [Fact]
    public async Task Consulta_de_tenants_sem_titular_devolve_vazio()
    {
        var tenants = new Mock<ITenantReadRepository>();

        var resultado = await new TenantsDisponiveisQueryHandler(tenants.Object)
            .HandleAsync(new TenantsDisponiveisQuery(Guid.Empty), CancellationToken.None);

        Assert.Empty(resultado.Tenants);

        tenants.Verify(
            r => r.ObterDisponiveisAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------------------------------------------------------------- entrada inválida

    /// <summary>
    /// Entrada inválida é recusada antes de qualquer ida ao banco. Não é só economia de
    /// consulta: um identificador vazio chegando ao repositório viraria um DELETE ou UPDATE
    /// que a RLS até barra, mas cujo erro chegaria ao cliente como falha de infraestrutura em
    /// vez de "faltou informar o quê".
    /// </summary>
    [Fact]
    public async Task Remocao_de_regra_sem_identificador_nao_chega_ao_repositorio()
    {
        var regras = new Mock<IRegraClassificacaoWriteRepository>();

        var resultado = await new RemoverRegraCommandHandler(regras.Object)
            .HandleAsync(new RemoverRegraCommand(Tenant, Guid.Empty), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        regras.Verify(
            r => r.RemoverAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Remocao_de_lancamento_sem_identificador_nao_chega_ao_repositorio()
    {
        var lancamentos = new Mock<ILancamentoWriteRepository>();

        var resultado = await new RemoverLancamentoCommandHandler(lancamentos.Object)
            .HandleAsync(new RemoverLancamentoCommand(Tenant, Guid.Empty), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        lancamentos.Verify(
            r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Encerramento_de_recorrencia_sem_identificador_nao_chega_ao_repositorio()
    {
        var recorrencias = new Mock<IRecorrenciaWriteRepository>();

        var resultado = await new EncerrarRecorrenciaCommandHandler(
                recorrencias.Object, new RelogioFixo())
            .HandleAsync(
                new EncerrarRecorrenciaCommand(Tenant, Guid.Empty), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        recorrencias.Verify(
            r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Revogacao_sem_destinatario_nao_chega_ao_repositorio()
    {
        var tenants = new Mock<ITenantWriteRepository>();

        var resultado = await new RevogarCompartilhamentoCommandHandler(
                tenants.Object, new RelogioFixo())
            .HandleAsync(
                new RevogarCompartilhamentoCommand(Usuario, null, Guid.Empty),
                CancellationToken.None);

        Assert.False(resultado.Sucesso);
        tenants.Verify(
            r => r.ObterTenantDoUsuarioAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Alteracao_de_lancamento_invalida_nao_chega_ao_repositorio()
    {
        var lancamentos = new Mock<ILancamentoWriteRepository>();

        var resultado = await new AlterarLancamentoCommandHandler(
                lancamentos.Object, Categorias(existe: true).Object, new RelogioFixo())
            .HandleAsync(
                new AlterarLancamentoCommand(Tenant, Guid.Empty, "", 0m, null, null, null),
                CancellationToken.None);

        Assert.False(resultado.Sucesso);
        lancamentos.Verify(
            r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Classificacao_invalida_nao_chega_ao_repositorio()
    {
        var lancamentos = new Mock<ILancamentoWriteRepository>();

        var resultado = await new ClassificarLancamentoCommandHandler(
                lancamentos.Object,
                Categorias(existe: true).Object,
                new Mock<IRegraClassificacaoWriteRepository>().Object,
                new RelogioFixo())
            .HandleAsync(
                new ClassificarLancamentoCommand(Tenant, Guid.Empty, Guid.Empty),
                CancellationToken.None);

        Assert.False(resultado.Sucesso);
        lancamentos.Verify(
            r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------------------------------------------------------------- consentimento

    [Fact]
    public async Task Consentimento_de_todos_os_documentos_na_versao_atual_esta_vigente()
    {
        var usuarios = new Mock<IUsuarioReadRepository>();
        usuarios.Setup(r => r.ObterConsentimentosAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. DocumentoConsentimento.Obrigatorios.Select(documento =>
                new ConsentimentoEntity
                {
                    Id = Guid.CreateVersion7(),
                    UsuarioId = Usuario,
                    Documento = documento,
                    Versao = "2.0",
                    AceitoEm = DateTimeOffset.UtcNow,
                })]);

        var resultado = await new ConsentimentoVigenteQueryHandler(usuarios.Object)
            .HandleAsync(new ConsentimentoVigenteQuery(Usuario, "2.0"), CancellationToken.None);

        Assert.True(resultado.Vigente);
        Assert.Empty(resultado.Pendentes);
    }

    /// <summary>
    /// Aceite de uma versão antiga não vale para a nova: a prova do consentimento é por versão,
    /// senão "o usuário aceitou" deixa de dizer o que ele aceitou.
    /// </summary>
    [Fact]
    public async Task Aceite_de_versao_anterior_deixa_o_documento_pendente()
    {
        var usuarios = new Mock<IUsuarioReadRepository>();
        usuarios.Setup(r => r.ObterConsentimentosAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ConsentimentoEntity
                {
                    Id = Guid.CreateVersion7(),
                    UsuarioId = Usuario,
                    Documento = DocumentoConsentimento.TermosDeUso,
                    Versao = "1.0",
                    AceitoEm = DateTimeOffset.UtcNow,
                },
            ]);

        var resultado = await new ConsentimentoVigenteQueryHandler(usuarios.Object)
            .HandleAsync(new ConsentimentoVigenteQuery(Usuario, "2.0"), CancellationToken.None);

        Assert.False(resultado.Vigente);
        Assert.Equal(DocumentoConsentimento.Obrigatorios.Length, resultado.Pendentes.Count);
    }

    /// <summary>
    /// Sem a versão vigente não há contra o que comparar o aceite: responder "está vigente"
    /// nesse estado liberaria a conta sem que se soubesse de qual documento se fala.
    /// </summary>
    [Fact]
    public async Task Consulta_de_consentimento_sem_versao_e_recusada()
    {
        var usuarios = new Mock<IUsuarioReadRepository>();

        var resultado = await new ConsentimentoVigenteQueryHandler(usuarios.Object)
            .HandleAsync(new ConsentimentoVigenteQuery(Usuario, "  "), CancellationToken.None);

        Assert.False(resultado.Vigente);
        Assert.NotEmpty(resultado.Erros);

        usuarios.Verify(
            r => r.ObterConsentimentosAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
