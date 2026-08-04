using Acme.Sistemas.Domain.Entities.Classificacao;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Services.V1.Classificacao;
using Acme.Sistemas.Services.V1.Classificacao.Command.AlterarRegra;
using Acme.Sistemas.Services.V1.Classificacao.Command.ClassificarLancamento;
using Acme.Sistemas.Services.V1.Classificacao.Command.RemoverRegra;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

public sealed class ClassificacaoTests
{
    private static readonly Guid Tenant = Guid.CreateVersion7();
    private static readonly Guid Alimentacao = Guid.CreateVersion7();
    private static readonly Guid Mercado = Guid.CreateVersion7();
    private static readonly Guid Lazer = Guid.CreateVersion7();

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);
    }

    private static RegraClassificacaoEntity Regra(string padrao, Guid categoria, int diaDeCriacao) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = Tenant,
            Padrao = padrao,
            CategoriaId = categoria,
            CriadoEm = new DateTimeOffset(2026, 7, diaDeCriacao, 0, 0, 0, TimeSpan.Zero),
        };

    private static ClassificacaoService Servico(params RegraClassificacaoEntity[] regras)
    {
        var repositorio = new Mock<IRegraClassificacaoWriteRepository>();
        repositorio.Setup(r => r.ListarAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(regras);
        return new ClassificacaoService(repositorio.Object);
    }

    [Fact]
    public async Task Regra_existente_casa_com_a_descricao()
    {
        var servico = Servico(Regra("IFOOD", Alimentacao, 1));

        var resultado = await servico.ClassificarAsync("PAG*IFOOD", CancellationToken.None);

        Assert.True(resultado.Classificou);
        Assert.Equal(Alimentacao, resultado.CategoriaId);
    }

    [Fact]
    public async Task Nenhuma_regra_casa_deixa_pendente()
    {
        var servico = Servico(Regra("IFOOD", Alimentacao, 1));

        var resultado = await servico.ClassificarAsync("POSTO SHELL", CancellationToken.None);

        Assert.False(resultado.Classificou);
        Assert.Null(resultado.CategoriaId);
    }

    [Fact]
    public async Task Duas_regras_casam_e_vence_a_mais_especifica()
    {
        // "IFOOD MERCADO" descreve um caso mais estreito que "IFOOD" e deve ganhar, mesmo
        // tendo sido criada antes.
        var servico = Servico(
            Regra("IFOOD", Alimentacao, 10),
            Regra("IFOOD MERCADO", Mercado, 1));

        var resultado = await servico.ClassificarAsync("PAG*IFOOD MERCADO SP", CancellationToken.None);

        Assert.Equal(Mercado, resultado.CategoriaId);
    }

    [Fact]
    public async Task Empate_de_especificidade_resolve_pela_mais_recente()
    {
        // Padrões de mesmo tamanho: vence a criada depois, que reflete a decisão mais atual.
        var servico = Servico(
            Regra("LOJA A", Alimentacao, 1),
            Regra("LOJA B", Lazer, 20));

        var resultado = await servico.ClassificarAsync("LOJA A E LOJA B", CancellationToken.None);

        Assert.Equal(Lazer, resultado.CategoriaId);
    }

    [Fact]
    public async Task Acento_e_caixa_nao_impedem_o_casamento()
    {
        // A fatura escreve a mesma loja de formas diferentes. Sem normalizar, a pessoa criaria
        // uma regra por variação e nenhuma casaria com a próxima.
        var servico = Servico(Regra(NormalizadorDeDescricao.Normalizar("Açaí"), Alimentacao, 1));

        var resultado = await servico.ClassificarAsync("pag*ACAI  DA  PRAIA", CancellationToken.None);

        Assert.Equal(Alimentacao, resultado.CategoriaId);
    }

    [Fact]
    public void Normalizacao_colapsa_espaco_e_remove_acento()
    {
        Assert.Equal("ACAI DA PRAIA", NormalizadorDeDescricao.Normalizar("  Açaí   da   Praia  "));
        Assert.Equal(string.Empty, NormalizadorDeDescricao.Normalizar("   "));
    }

    [Fact]
    public async Task Descricao_vazia_nao_classifica()
    {
        // Um padrão vazio casaria com tudo. Vale a pena garantir que nem a descrição vazia
        // chega a consultar as regras.
        var repositorio = new Mock<IRegraClassificacaoWriteRepository>();
        var servico = new ClassificacaoService(repositorio.Object);

        var resultado = await servico.ClassificarAsync("   ", CancellationToken.None);

        Assert.False(resultado.Classificou);
        repositorio.Verify(r => r.ListarAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (Mock<ILancamentoWriteRepository>, LancamentoEntity) Lancamento(
        Guid? categoria = null,
        OrigemClassificacao origem = OrigemClassificacao.Pendente,
        string descricao = "PAG*XYZ")
    {
        var lancamento = new LancamentoEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = Tenant,
            Descricao = descricao,
            Valor = 120m,
            DataCompetencia = new DateOnly(2026, 7, 15),
            DataCaixa = new DateOnly(2026, 7, 15),
            CategoriaId = categoria,
            OrigemClassificacao = origem,
            CriadoEm = DateTimeOffset.UtcNow,
        };

        var repositorio = new Mock<ILancamentoWriteRepository>();
        repositorio.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lancamento);
        repositorio.Setup(r => r.AlterarAsync(It.IsAny<LancamentoEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return (repositorio, lancamento);
    }

    private static Mock<ICategoriaWriteRepository> Categorias(bool existe = true)
    {
        var categorias = new Mock<ICategoriaWriteRepository>();
        categorias.Setup(r => r.ExisteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existe);
        return categorias;
    }

    [Fact]
    public async Task Classificacao_manual_marca_a_origem_e_devolve_a_competencia()
    {
        var (lancamentos, lancamento) = Lancamento();
        var regras = new Mock<IRegraClassificacaoWriteRepository>();

        var handler = new ClassificarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, regras.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new ClassificarLancamentoCommand(Tenant, lancamento.Id, Lazer), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(new DateOnly(2026, 7, 15), resultado.CompetenciaAfetada);

        // A marca de manual é o que impede qualquer regra futura de desfazer a decisão.
        Assert.Equal(OrigemClassificacao.Manual, lancamento.OrigemClassificacao);
        Assert.Equal(Lazer, lancamento.CategoriaId);
    }

    [Fact]
    public async Task Correcao_aceita_vira_regra_com_o_padrao_normalizado()
    {
        var (lancamentos, lancamento) = Lancamento(descricao: "PAG*XYZ Café");
        var regras = new Mock<IRegraClassificacaoWriteRepository>();

        var handler = new ClassificarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, regras.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new ClassificarLancamentoCommand(Tenant, lancamento.Id, Lazer, CriarRegra: true),
            CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("PAG*XYZ CAFE", resultado.PadraoDaRegra);
        regras.Verify(
            r => r.GravarAsync(
                It.Is<RegraClassificacaoEntity>(regra =>
                    regra.Padrao == "PAG*XYZ CAFE" && regra.CategoriaId == Lazer),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Correcao_recusada_classifica_so_aquele_lancamento()
    {
        var (lancamentos, lancamento) = Lancamento();
        var regras = new Mock<IRegraClassificacaoWriteRepository>();

        var handler = new ClassificarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, regras.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new ClassificarLancamentoCommand(Tenant, lancamento.Id, Lazer), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Null(resultado.RegraId);

        // Sem regra, os futuros equivalentes continuam caindo em pendente — que é o que a
        // pessoa pediu ao recusar.
        regras.Verify(
            r => r.GravarAsync(It.IsAny<RegraClassificacaoEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Trocar_categoria_ja_atribuida_substitui_a_regra_do_padrao()
    {
        // O lançamento já tinha sido classificado por regra. A pessoa corrige e aceita criar
        // a regra: a associação anterior daquele padrão é que estava errada.
        var (lancamentos, lancamento) = Lancamento(
            categoria: Alimentacao, origem: OrigemClassificacao.Regra, descricao: "PAG*XYZ");

        var regras = new Mock<IRegraClassificacaoWriteRepository>();

        var handler = new ClassificarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, regras.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new ClassificarLancamentoCommand(Tenant, lancamento.Id, Lazer, CriarRegra: true),
            CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(Alimentacao, resultado.CategoriaAnteriorId);

        // Gravar, e não recusar por duplicidade: o repositório substitui a categoria do padrão.
        regras.Verify(
            r => r.GravarAsync(
                It.Is<RegraClassificacaoEntity>(regra =>
                    regra.Padrao == "PAG*XYZ" && regra.CategoriaId == Lazer),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Categoria_inexistente_recusa_a_classificacao()
    {
        var (lancamentos, lancamento) = Lancamento();
        var regras = new Mock<IRegraClassificacaoWriteRepository>();

        var handler = new ClassificarLancamentoCommandHandler(
            lancamentos.Object, Categorias(existe: false).Object, regras.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new ClassificarLancamentoCommand(Tenant, lancamento.Id, Lazer), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            ClassificarLancamentoCommandHandler.CodigoCategoriaInexistente, resultado.Erros[0].Codigo);
        lancamentos.Verify(
            r => r.AlterarAsync(It.IsAny<LancamentoEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Descricao_sem_padrao_nao_pode_virar_regra()
    {
        // Padrão vazio casaria com tudo e classificaria o tenant inteiro na gravação seguinte.
        var (lancamentos, lancamento) = Lancamento(descricao: "   ");
        var regras = new Mock<IRegraClassificacaoWriteRepository>();

        var handler = new ClassificarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, regras.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new ClassificarLancamentoCommand(Tenant, lancamento.Id, Lazer, CriarRegra: true),
            CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            ClassificarLancamentoCommandHandler.CodigoDescricaoSemPadrao, resultado.Erros[0].Codigo);
        regras.Verify(
            r => r.GravarAsync(It.IsAny<RegraClassificacaoEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Regra_nova_nao_reclassifica_lancamento_ja_gravado()
    {
        // Criar regra grava a regra e nada mais: nenhum caminho toca em lançamento existente.
        // É o que garante que uma regra nova não altere o que a pessoa classificou à mão.
        var (lancamentos, lancamento) = Lancamento(
            categoria: Alimentacao, origem: OrigemClassificacao.Manual);

        var regras = new Mock<IRegraClassificacaoWriteRepository>();

        var handler = new ClassificarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, regras.Object, new RelogioFixo());

        await handler.HandleAsync(
            new ClassificarLancamentoCommand(Tenant, lancamento.Id, Lazer, CriarRegra: true),
            CancellationToken.None);

        // Uma única alteração: a do lançamento que a pessoa estava corrigindo.
        lancamentos.Verify(
            r => r.AlterarAsync(It.IsAny<LancamentoEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Padrao_curto_demais_e_recusado_na_alteracao()
    {
        var regras = new Mock<IRegraClassificacaoWriteRepository>();
        var handler = new AlterarRegraCommandHandler(regras.Object, Categorias().Object);

        var resultado = await handler.HandleAsync(
            new AlterarRegraCommand(Tenant, Guid.CreateVersion7(), "AB", Lazer),
            CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(AlterarRegraCommandValidation.CodigoPadraoMuitoCurto, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Alteracao_normaliza_o_padrao_antes_de_gravar()
    {
        var regraExistente = Regra("IFOOD", Alimentacao, 1);

        var regras = new Mock<IRegraClassificacaoWriteRepository>();
        regras.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(regraExistente);
        regras.Setup(r => r.AlterarAsync(
                It.IsAny<RegraClassificacaoEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new AlterarRegraCommandHandler(regras.Object, Categorias().Object);

        var resultado = await handler.HandleAsync(
            new AlterarRegraCommand(Tenant, regraExistente.Id, " Padaria  São  João ", Lazer),
            CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("PADARIA SAO JOAO", resultado.Padrao);
    }

    [Fact]
    public async Task Remover_regra_inexistente_e_recusado()
    {
        var regras = new Mock<IRegraClassificacaoWriteRepository>();
        regras.Setup(r => r.RemoverAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new RemoverRegraCommandHandler(regras.Object);

        var resultado = await handler.HandleAsync(
            new RemoverRegraCommand(Tenant, Guid.CreateVersion7()), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            RemoverRegraCommandHandler.CodigoRegraNaoEncontrada, resultado.Erros[0].Codigo);
    }
}
