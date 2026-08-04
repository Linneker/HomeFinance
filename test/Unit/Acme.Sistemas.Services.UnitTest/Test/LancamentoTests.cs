using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Services.V1.Lancamentos.Command.AlterarLancamento;
using Acme.Sistemas.Services.V1.Lancamentos.Command.CriarLancamento;
using Acme.Sistemas.Services.V1.Lancamentos.Command.RemoverLancamento;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

public sealed class LancamentoTests
{
    private static readonly Guid Tenant = Guid.CreateVersion7();
    private static readonly Guid Ana = Guid.CreateVersion7();
    private static readonly Guid Alimentacao = Guid.CreateVersion7();

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);
    }

    /// <summary>Guarda o que foi gravado para o teste inspecionar depois.</summary>
    private sealed class Gravacao
    {
        public List<LancamentoEntity> Lancamentos { get; } = [];

        public List<LancamentoParticipacaoEntity> Participacoes { get; } = [];
    }

    private static (Mock<ILancamentoWriteRepository> Repositorio, Gravacao Gravado) Repositorio(
        int semelhantes = 0)
    {
        var gravado = new Gravacao();
        var lancamentos = new Mock<ILancamentoWriteRepository>();

        lancamentos.Setup(r => r.ContarSemelhantesAsync(
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(semelhantes);

        lancamentos.Setup(r => r.CriarAsync(
                It.IsAny<IReadOnlyList<LancamentoEntity>>(),
                It.IsAny<IReadOnlyList<LancamentoParticipacaoEntity>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<LancamentoEntity>, IReadOnlyList<LancamentoParticipacaoEntity>,
                CancellationToken>((novos, participacoes, _) =>
            {
                gravado.Lancamentos.AddRange(novos);
                gravado.Participacoes.AddRange(participacoes);
            })
            .Returns(Task.CompletedTask);

        return (lancamentos, gravado);
    }

    private static Mock<ICategoriaWriteRepository> Categorias(bool existe = true)
    {
        var categorias = new Mock<ICategoriaWriteRepository>();
        categorias.Setup(r => r.ExisteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existe);
        return categorias;
    }

    private static CriarLancamentoCommand Comando(
        decimal valor = 240m,
        DateOnly? competencia = null,
        DateOnly? caixa = null,
        Guid? categoria = null,
        int parcelas = 1,
        bool confirmou = false) =>
        new(Tenant,
            Ana,
            "Mercado",
            valor,
            competencia ?? new DateOnly(2026, 7, 3),
            caixa ?? new DateOnly(2026, 7, 3),
            categoria,
            parcelas,
            confirmou);

    [Fact]
    public async Task Lancamento_a_vista_grava_as_duas_datas_iguais()
    {
        var (lancamentos, gravado) = Repositorio();
        var handler = new CriarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            Comando(caixa: new DateOnly(2026, 7, 3)), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        var unico = Assert.Single(gravado.Lancamentos);
        Assert.Equal(new DateOnly(2026, 7, 3), unico.DataCompetencia);
        Assert.Equal(new DateOnly(2026, 7, 3), unico.DataCaixa);
        Assert.Equal(TipoLancamento.Avulso, unico.Tipo);
    }

    [Fact]
    public async Task Consumo_em_um_mes_com_pagamento_em_outro_mantem_as_duas_datas()
    {
        var (lancamentos, gravado) = Repositorio();
        var handler = new CriarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, new RelogioFixo());

        // Conta de luz do consumo de julho vencendo em 10/08: é o caso que justifica guardar
        // as duas datas. Com uma só, ou o mês de julho perde o gasto ou agosto ganha um que
        // não é dele.
        await handler.HandleAsync(
            Comando(competencia: new DateOnly(2026, 7, 31), caixa: new DateOnly(2026, 8, 10)),
            CancellationToken.None);

        var unico = Assert.Single(gravado.Lancamentos);
        Assert.Equal(new DateOnly(2026, 7, 31), unico.DataCompetencia);
        Assert.Equal(new DateOnly(2026, 8, 10), unico.DataCaixa);
    }

    [Fact]
    public async Task Data_ausente_e_recusada_dizendo_qual_falta()
    {
        var (lancamentos, _) = Repositorio();
        var handler = new CriarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, new RelogioFixo());

        var semCaixa = await handler.HandleAsync(
            Comando() with { DataCaixa = null }, CancellationToken.None);

        var semCompetencia = await handler.HandleAsync(
            Comando() with { DataCompetencia = null }, CancellationToken.None);

        Assert.Equal(
            CriarLancamentoCommandValidation.CodigoCaixaObrigatoria, semCaixa.Erros[0].Codigo);
        Assert.Equal(
            CriarLancamentoCommandValidation.CodigoCompetenciaObrigatoria,
            semCompetencia.Erros[0].Codigo);

        lancamentos.Verify(
            r => r.CriarAsync(
                It.IsAny<IReadOnlyList<LancamentoEntity>>(),
                It.IsAny<IReadOnlyList<LancamentoParticipacaoEntity>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Categoria_inexistente_e_recusada()
    {
        var (lancamentos, _) = Repositorio();
        var handler = new CriarLancamentoCommandHandler(
            lancamentos.Object, Categorias(existe: false).Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            Comando(categoria: Alimentacao), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            CriarLancamentoCommandHandler.CodigoCategoriaInexistente, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Lancamento_sem_categoria_nasce_pendente_de_classificacao()
    {
        var (lancamentos, gravado) = Repositorio();
        var handler = new CriarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, new RelogioFixo());

        await handler.HandleAsync(Comando(), CancellationToken.None);

        var unico = Assert.Single(gravado.Lancamentos);
        Assert.True(unico.PendenteDeClassificacao);
        Assert.Equal(OrigemClassificacao.Pendente, unico.OrigemClassificacao);
    }

    [Fact]
    public async Task Participacao_unica_de_cem_por_cento_nasce_com_o_lancamento()
    {
        var (lancamentos, gravado) = Repositorio();
        var handler = new CriarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, new RelogioFixo());

        await handler.HandleAsync(Comando(), CancellationToken.None);

        var participacao = Assert.Single(gravado.Participacoes);
        Assert.Equal(Ana, participacao.UsuarioId);
        Assert.Equal(100m, participacao.Percentual);
        Assert.Equal(gravado.Lancamentos[0].Id, participacao.LancamentoId);
    }

    [Fact]
    public async Task Compra_em_tres_parcelas_soma_integral_na_competencia_e_distribui_o_caixa()
    {
        var (lancamentos, gravado) = Repositorio();
        var handler = new CriarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, new RelogioFixo());

        // Compra de R$ 300,00 em 20/07, três parcelas vencendo em agosto, setembro e outubro.
        var resultado = await handler.HandleAsync(
            Comando(
                valor: 300m,
                competencia: new DateOnly(2026, 7, 20),
                caixa: new DateOnly(2026, 8, 20),
                parcelas: 3),
            CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(3, gravado.Lancamentos.Count);

        // Competência: os R$ 300,00 inteiros ficam em julho.
        Assert.All(
            gravado.Lancamentos,
            lancamento => Assert.Equal(new DateOnly(2026, 7, 20), lancamento.DataCompetencia));
        Assert.Equal(300m, gravado.Lancamentos.Sum(lancamento => lancamento.Valor));

        // Caixa: três saídas de R$ 100,00, uma por mês.
        Assert.Equal(
            [new DateOnly(2026, 8, 20), new DateOnly(2026, 9, 20), new DateOnly(2026, 10, 20)],
            gravado.Lancamentos.Select(lancamento => lancamento.DataCaixa));
        Assert.All(gravado.Lancamentos, lancamento => Assert.Equal(100m, lancamento.Valor));

        // Ligadas pelo mesmo grupo, para a tela exibir "1/3".
        Assert.Single(gravado.Lancamentos.Select(l => l.GrupoParcelamentoId).Distinct());
        Assert.Equal(["1/3", "2/3", "3/3"], gravado.Lancamentos.Select(l => l.Parcelamento));

        // Uma participação por parcela: a apuração por pessoa não pode perder duas das três.
        Assert.Equal(3, gravado.Participacoes.Count);
    }

    [Fact]
    public void Parcela_com_centavo_quebrado_ainda_soma_o_total_exato()
    {
        // R$ 100,00 em 3 não divide. Um centavo perdido apareceria como diferença
        // inexplicável entre o extrato e a fatura do cartão.
        var valores = RateioDeParcelas.Dividir(100m, 3);

        Assert.Equal(100m, valores.Sum());
        Assert.Equal([33.34m, 33.33m, 33.33m], valores);
    }

    [Fact]
    public void Serie_mensal_conta_sempre_da_origem_e_nao_encolhe_no_mes_curto()
    {
        // Somando um mês de cada vez, 31/01 viraria 28/02 e a série ficaria presa no dia 28
        // para sempre. Contando da origem, março volta ao dia 31.
        var datas = CalendarioFinanceiro.SerieMensal(new DateOnly(2026, 1, 31), 4);

        Assert.Equal(
            [
                new DateOnly(2026, 1, 31),
                new DateOnly(2026, 2, 28),
                new DateOnly(2026, 3, 31),
                new DateOnly(2026, 4, 30),
            ],
            datas);
    }

    [Fact]
    public async Task Duplicidade_detectada_exige_confirmacao_e_nao_grava()
    {
        var (lancamentos, gravado) = Repositorio(semelhantes: 1);
        var handler = new CriarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(Comando(valor: 280m), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.True(resultado.AguardandoConfirmacaoDeDuplicidade);
        Assert.Equal(1, resultado.SemelhantesEncontrados);
        Assert.Empty(resultado.Erros);
        Assert.Empty(gravado.Lancamentos);
    }

    [Fact]
    public async Task Duplicidade_confirmada_grava_normalmente()
    {
        var (lancamentos, gravado) = Repositorio(semelhantes: 1);
        var handler = new CriarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            Comando(valor: 280m, confirmou: true), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Single(gravado.Lancamentos);

        // Confirmada, a verificação nem chega a rodar: a resposta já é conhecida.
        lancamentos.Verify(
            r => r.ContarSemelhantesAsync(
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Alteracao_que_muda_a_competencia_devolve_os_dois_meses_a_reapurar()
    {
        var lancamentos = new Mock<ILancamentoWriteRepository>();
        lancamentos.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LancamentoEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = Tenant,
                Descricao = "Mercado",
                Valor = 240m,
                DataCompetencia = new DateOnly(2026, 7, 3),
                DataCaixa = new DateOnly(2026, 7, 3),
                CategoriaId = Alimentacao,
                OrigemClassificacao = OrigemClassificacao.Manual,
                CriadoEm = DateTimeOffset.UtcNow,
            });
        lancamentos.Setup(r => r.AlterarAsync(It.IsAny<LancamentoEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new AlterarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new AlterarLancamentoCommand(
                Tenant,
                Guid.CreateVersion7(),
                "Mercado",
                300m,
                new DateOnly(2026, 8, 3),
                new DateOnly(2026, 8, 3),
                Alimentacao),
            CancellationToken.None);

        Assert.True(resultado.Sucesso);

        // Reapurar só o mês novo deixaria julho estourado para sempre por um gasto que saiu de lá.
        Assert.Equal(
            [new DateOnly(2026, 7, 3), new DateOnly(2026, 8, 3)], resultado.CompetenciasAfetadas);
    }

    [Fact]
    public async Task Alteracao_sem_mudar_a_competencia_devolve_apenas_um_mes()
    {
        var lancamentos = new Mock<ILancamentoWriteRepository>();
        lancamentos.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LancamentoEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = Tenant,
                Descricao = "Mercado",
                Valor = 240m,
                DataCompetencia = new DateOnly(2026, 7, 3),
                DataCaixa = new DateOnly(2026, 7, 3),
                CriadoEm = DateTimeOffset.UtcNow,
            });
        lancamentos.Setup(r => r.AlterarAsync(It.IsAny<LancamentoEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new AlterarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new AlterarLancamentoCommand(
                Tenant, Guid.CreateVersion7(), "Mercado", 300m,
                new DateOnly(2026, 7, 3), new DateOnly(2026, 7, 3), null),
            CancellationToken.None);

        Assert.Single(resultado.CompetenciasAfetadas);
    }

    [Fact]
    public async Task Ocorrencia_ainda_prevista_nao_e_editavel()
    {
        var lancamentos = new Mock<ILancamentoWriteRepository>();
        lancamentos.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LancamentoEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = Tenant,
                Descricao = "Internet",
                Valor = 120m,
                DataCompetencia = new DateOnly(2026, 12, 10),
                DataCaixa = new DateOnly(2026, 12, 10),
                RecorrenciaId = Guid.CreateVersion7(),
                Previsto = true,
                CriadoEm = DateTimeOffset.UtcNow,
            });

        var handler = new AlterarLancamentoCommandHandler(
            lancamentos.Object, Categorias().Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new AlterarLancamentoCommand(
                Tenant, Guid.CreateVersion7(), "Internet", 130m,
                new DateOnly(2026, 12, 10), new DateOnly(2026, 12, 10), null),
            CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            AlterarLancamentoCommandHandler.CodigoOcorrenciaPrevista, resultado.Erros[0].Codigo);
        lancamentos.Verify(
            r => r.AlterarAsync(It.IsAny<LancamentoEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Remocao_devolve_a_competencia_e_a_categoria_a_reapurar()
    {
        var lancamentos = new Mock<ILancamentoWriteRepository>();
        lancamentos.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LancamentoEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = Tenant,
                Descricao = "Mercado",
                Valor = 240m,
                DataCompetencia = new DateOnly(2026, 7, 3),
                DataCaixa = new DateOnly(2026, 8, 5),
                CategoriaId = Alimentacao,
                CriadoEm = DateTimeOffset.UtcNow,
            });
        lancamentos.Setup(r => r.RemoverAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new RemoverLancamentoCommandHandler(lancamentos.Object);

        var resultado = await handler.HandleAsync(
            new RemoverLancamentoCommand(Tenant, Guid.CreateVersion7()), CancellationToken.None);

        Assert.True(resultado.Sucesso);

        // Carrega antes de apagar porque depois do DELETE não há de onde tirar isto, e sem
        // isto o orçamento continuaria contando um gasto que já não existe.
        Assert.Equal(new DateOnly(2026, 7, 3), resultado.CompetenciaAfetada);
        Assert.Equal(Alimentacao, resultado.CategoriaAfetadaId);
    }

    [Fact]
    public async Task Remover_o_que_nao_existe_e_recusado()
    {
        var lancamentos = new Mock<ILancamentoWriteRepository>();
        lancamentos.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LancamentoEntity?)null);

        var handler = new RemoverLancamentoCommandHandler(lancamentos.Object);

        var resultado = await handler.HandleAsync(
            new RemoverLancamentoCommand(Tenant, Guid.CreateVersion7()), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            RemoverLancamentoCommandHandler.CodigoLancamentoNaoEncontrado, resultado.Erros[0].Codigo);
        lancamentos.Verify(
            r => r.RemoverAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
