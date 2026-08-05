using Acme.Sistemas.Domain.Entities.Planejamento;
using Acme.Sistemas.Domain.Entities.Relatorios;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Services.V1.Relatorios.Query.ExtratoDoMes;
using Acme.Sistemas.Services.V1.Relatorios.Query.ResumoDoMes;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

/// <summary>
/// Extrato e resumo do mês, nas duas visões.
/// </summary>
public sealed class RelatorioTests
{
    private static readonly Guid Tenant = Guid.CreateVersion7();
    private static readonly Guid OutroTenant = Guid.CreateVersion7();
    private static readonly Guid Alimentacao = Guid.CreateVersion7();
    private static readonly Guid Transporte = Guid.CreateVersion7();
    private static readonly Guid Lazer = Guid.CreateVersion7();

    private static readonly DateOnly Julho = new(2026, 7, 1);
    private static readonly DateOnly Agosto = new(2026, 8, 1);

    /// <summary>
    /// Dublê fiel ao contrato da consulta: filtra pela coluna da visão ativa, soma o apurado e
    /// tira os previstos do total por categoria. Escrever o filtro aqui é o que faz o teste da
    /// compra parcelada valer alguma coisa — um mock que devolvesse lista fixa provaria apenas
    /// que o handler repassa o que recebeu.
    /// </summary>
    private sealed class RelatorioFalso(params LancamentoApuradoEntity[] lancamentos)
        : IRelatorioReadRepository
    {
        public Task<IReadOnlyList<LancamentoApuradoEntity>> ExtratoAsync(
            PeriodoDoRelatorio periodo,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LancamentoApuradoEntity>>([.. NoPeriodo(periodo)]);

        public Task<IReadOnlyList<TotalPorCategoria>> TotaisPorCategoriaAsync(
            PeriodoDoRelatorio periodo,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TotalPorCategoria>>(
                [.. NoPeriodo(periodo)
                    .Where(linha => !linha.Previsto)
                    .GroupBy(linha => linha.CategoriaId)
                    .Select(grupo => new TotalPorCategoria(
                        grupo.Key, grupo.Sum(linha => linha.ValorApurado), grupo.Count()))]);

        private IEnumerable<LancamentoApuradoEntity> NoPeriodo(PeriodoDoRelatorio periodo) =>
            lancamentos.Where(linha =>
            {
                var data = periodo.PorCaixa ? linha.DataCaixa : linha.DataCompetencia;
                return data >= periodo.De && data <= periodo.Ate;
            });
    }

    private static LancamentoApuradoEntity Linha(
        string descricao,
        decimal valor,
        DateOnly competencia,
        DateOnly caixa,
        Guid? categoria = null,
        int? parcelaNumero = null,
        int? parcelaTotal = null,
        Guid? grupo = null,
        bool previsto = false,
        Guid? tenant = null) => new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant ?? Tenant,
            Descricao = descricao,
            Valor = valor,
            ValorApurado = valor,
            DataCompetencia = competencia,
            DataCaixa = caixa,
            CategoriaId = categoria,
            ParcelaNumero = parcelaNumero,
            ParcelaTotal = parcelaTotal,
            GrupoParcelamentoId = grupo,
            Previsto = previsto,
            CriadoEm = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
        };

    private static OrcamentoMensalEntity Orcado(Guid categoria, DateOnly mes, decimal valor) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Tenant,
        CategoriaId = categoria,
        Competencia = mes,
        ValorPlanejado = valor,
        CriadoEm = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
    };

    private static Mock<IOrcamentoReadRepository> Orcamentos(
        params OrcamentoMensalEntity[] orcamentos)
    {
        var repositorio = new Mock<IOrcamentoReadRepository>();

        repositorio
            .Setup(r => r.ListarDoMesAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orcamentos);

        return repositorio;
    }

    /// <summary>Compra de R$ 300 em 3x feita em julho, como o grupo 5 a grava.</summary>
    private static LancamentoApuradoEntity[] CompraParceladaEmJulho() =>
        [.. Enumerable.Range(0, 3).Select(indice =>
        {
            var grupo = new Guid("11111111-1111-1111-1111-111111111111");

            return Linha(
                "Geladeira",
                100m,
                Julho,
                Julho.AddMonths(indice),
                Alimentacao,
                indice + 1,
                3,
                grupo);
        })];

    // ---------------------------------------------------------------- competência x caixa

    /// <summary>
    /// Cenário literal da spec. A compra pertence a julho: é quando o gasto aconteceu, e as três
    /// parcelas compartilham a competência.
    /// </summary>
    [Fact]
    public async Task Compra_parcelada_aparece_integral_em_julho_na_competencia()
    {
        var handler = new ExtratoDoMesQueryHandler(
            new RelatorioFalso(CompraParceladaEmJulho()), Orcamentos().Object);

        var resultado = await handler.HandleAsync(
            new ExtratoDoMesQuery(Tenant, Julho), CancellationToken.None);

        Assert.False(resultado.VisaoPorCaixa);
        Assert.Equal(3, resultado.Lancamentos.Count);
        Assert.Equal(300m, resultado.Total);
    }

    /// <summary>
    /// A mesma compra, em caixa e em agosto, é só a parcela que vence ali. É o que separa "o que
    /// eu gastei" de "o que sai da conta este mês".
    /// </summary>
    [Fact]
    public async Task Mesma_compra_aparece_como_uma_parcela_em_agosto_no_caixa()
    {
        var handler = new ExtratoDoMesQueryHandler(
            new RelatorioFalso(CompraParceladaEmJulho()), Orcamentos().Object);

        var resultado = await handler.HandleAsync(
            new ExtratoDoMesQuery(Tenant, Agosto, PorCaixa: true), CancellationToken.None);

        Assert.True(resultado.VisaoPorCaixa);
        Assert.Single(resultado.Lancamentos);
        Assert.Equal(100m, resultado.Total);
        Assert.Equal("2/3", resultado.Lancamentos[0].Parcelamento);
        Assert.Equal(TipoLancamento.Parcela, resultado.Lancamentos[0].Tipo);
    }

    [Fact]
    public async Task A_mesma_compra_em_julho_no_caixa_traz_apenas_a_primeira_parcela()
    {
        var handler = new ExtratoDoMesQueryHandler(
            new RelatorioFalso(CompraParceladaEmJulho()), Orcamentos().Object);

        var resultado = await handler.HandleAsync(
            new ExtratoDoMesQuery(Tenant, Julho, PorCaixa: true), CancellationToken.None);

        Assert.Single(resultado.Lancamentos);
        Assert.Equal("1/3", resultado.Lancamentos[0].Parcelamento);
    }

    [Fact]
    public async Task Competencia_e_a_visao_padrao_quando_nada_e_pedido()
    {
        var handler = new ExtratoDoMesQueryHandler(new RelatorioFalso(), Orcamentos().Object);

        var resultado = await handler.HandleAsync(
            new ExtratoDoMesQuery(Tenant, Agosto), CancellationToken.None);

        Assert.False(resultado.VisaoPorCaixa);
    }

    // ---------------------------------------------------------------- extrato

    [Fact]
    public async Task Lancamento_com_orcamento_no_mes_fica_vinculado_a_previsao()
    {
        var handler = new ExtratoDoMesQueryHandler(
            new RelatorioFalso(Linha("Mercado", 200m, Agosto, Agosto, Alimentacao)),
            Orcamentos(Orcado(Alimentacao, Agosto, 1200m)).Object);

        var resultado = await handler.HandleAsync(
            new ExtratoDoMesQuery(Tenant, Agosto), CancellationToken.None);

        var item = Assert.Single(resultado.Lancamentos);

        Assert.NotNull(item.OrcamentoId);
        Assert.Equal(1200m, item.ValorPlanejadoDaCategoria);
        Assert.False(item.ForaDoPlanejamento);
        Assert.False(item.PendenteDeClassificacao);
    }

    [Fact]
    public async Task Lancamento_em_categoria_sem_orcamento_e_marcado_fora_do_planejamento()
    {
        var handler = new ExtratoDoMesQueryHandler(
            new RelatorioFalso(Linha("Cinema", 60m, Agosto, Agosto, Lazer)),
            Orcamentos(Orcado(Alimentacao, Agosto, 1200m)).Object);

        var resultado = await handler.HandleAsync(
            new ExtratoDoMesQuery(Tenant, Agosto), CancellationToken.None);

        var item = Assert.Single(resultado.Lancamentos);

        Assert.True(item.ForaDoPlanejamento);
        Assert.Null(item.OrcamentoId);
        Assert.Null(item.ValorPlanejadoDaCategoria);
    }

    /// <summary>
    /// Pendente e fora do planejamento são coisas diferentes: ali ainda não se sabe a que plano
    /// o gasto pertenceria, aqui já se sabe que não pertence a nenhum. Marcar as duas juntas
    /// mandaria a pessoa mexer no orçamento quando o que falta é classificar.
    /// </summary>
    [Fact]
    public async Task Lancamento_sem_categoria_e_pendente_e_nao_fora_do_planejamento()
    {
        var handler = new ExtratoDoMesQueryHandler(
            new RelatorioFalso(Linha("PAG*XYZ", 45m, Agosto, Agosto)),
            Orcamentos(Orcado(Alimentacao, Agosto, 1200m)).Object);

        var resultado = await handler.HandleAsync(
            new ExtratoDoMesQuery(Tenant, Agosto), CancellationToken.None);

        var item = Assert.Single(resultado.Lancamentos);

        Assert.True(item.PendenteDeClassificacao);
        Assert.False(item.ForaDoPlanejamento);
        Assert.Equal(1, resultado.QuantidadePendente);
    }

    [Fact]
    public async Task Mes_sem_lancamentos_devolve_extrato_vazio_declarado()
    {
        var handler = new ExtratoDoMesQueryHandler(
            new RelatorioFalso(Linha("Mercado", 200m, Julho, Julho, Alimentacao)),
            Orcamentos().Object);

        var resultado = await handler.HandleAsync(
            new ExtratoDoMesQuery(Tenant, Agosto), CancellationToken.None);

        Assert.True(resultado.Vazio);
        Assert.Empty(resultado.Lancamentos);
        Assert.Equal(0m, resultado.Total);
        Assert.Equal(Agosto, resultado.Competencia);
    }

    [Fact]
    public async Task Extrato_sem_tenant_devolve_vazio()
    {
        var relatorios = new Mock<IRelatorioReadRepository>();

        var handler = new ExtratoDoMesQueryHandler(relatorios.Object, Orcamentos().Object);

        var resultado = await handler.HandleAsync(
            new ExtratoDoMesQuery(Guid.Empty, Agosto), CancellationToken.None);

        Assert.True(resultado.Vazio);

        relatorios.Verify(
            r => r.ExtratoAsync(It.IsAny<PeriodoDoRelatorio>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Competencia_informada_com_dia_qualquer_e_reduzida_ao_mes()
    {
        var handler = new ExtratoDoMesQueryHandler(
            new RelatorioFalso(Linha("Mercado", 200m, new DateOnly(2026, 8, 31), Agosto, Alimentacao)),
            Orcamentos().Object);

        var resultado = await handler.HandleAsync(
            new ExtratoDoMesQuery(Tenant, new DateOnly(2026, 8, 15)), CancellationToken.None);

        Assert.Equal(Agosto, resultado.Competencia);
        Assert.Single(resultado.Lancamentos);
    }

    // ---------------------------------------------------------------- resumo

    /// <summary>Cenário literal da spec: 1.200 planejados, 980 realizados, 220 disponíveis.</summary>
    [Fact]
    public async Task Categoria_dentro_do_orcamento_mostra_o_saldo_disponivel()
    {
        var handler = new ResumoDoMesQueryHandler(
            new RelatorioFalso(Linha("Mercado", 980m, Agosto, Agosto, Alimentacao)),
            Orcamentos(Orcado(Alimentacao, Agosto, 1200m)).Object);

        var resultado = await handler.HandleAsync(
            new ResumoDoMesQuery(Tenant, Agosto), CancellationToken.None);

        var linha = Assert.Single(resultado.Categorias);

        Assert.Equal(1200m, linha.Planejado);
        Assert.Equal(980m, linha.Realizado);
        Assert.Equal(220m, linha.Disponivel);
        Assert.Equal(0m, linha.Excedente);
        Assert.False(linha.Excedida);
    }

    /// <summary>Cenário literal da spec: 400 planejados, 520 realizados, 120 excedidos.</summary>
    [Fact]
    public async Task Categoria_estourada_mostra_o_excedente()
    {
        var handler = new ResumoDoMesQueryHandler(
            new RelatorioFalso(Linha("Combustível", 520m, Agosto, Agosto, Transporte)),
            Orcamentos(Orcado(Transporte, Agosto, 400m)).Object);

        var resultado = await handler.HandleAsync(
            new ResumoDoMesQuery(Tenant, Agosto), CancellationToken.None);

        var linha = Assert.Single(resultado.Categorias);

        Assert.True(linha.Excedida);
        Assert.Equal(120m, linha.Excedente);
        Assert.Equal(0m, linha.Disponivel);
        Assert.Equal(-120m, resultado.Diferenca);
    }

    /// <summary>
    /// Categoria orçada e não usada precisa aparecer: o que foi planejado e sobrou é metade da
    /// informação de um orçamento.
    /// </summary>
    [Fact]
    public async Task Categoria_orcada_sem_gasto_aparece_zerada()
    {
        var handler = new ResumoDoMesQueryHandler(
            new RelatorioFalso(), Orcamentos(Orcado(Alimentacao, Agosto, 1200m)).Object);

        var resultado = await handler.HandleAsync(
            new ResumoDoMesQuery(Tenant, Agosto), CancellationToken.None);

        var linha = Assert.Single(resultado.Categorias);

        Assert.Equal(0m, linha.Realizado);
        Assert.Equal(0, linha.Quantidade);
        Assert.Equal(1200m, linha.Disponivel);
    }

    [Fact]
    public async Task Gastos_em_categoria_sem_plano_vao_para_o_bloco_fora_do_planejamento()
    {
        var handler = new ResumoDoMesQueryHandler(
            new RelatorioFalso(
                Linha("Mercado", 980m, Agosto, Agosto, Alimentacao),
                Linha("Cinema", 60m, Agosto, Agosto, Lazer),
                Linha("PAG*XYZ", 40m, Agosto, Agosto)),
            Orcamentos(Orcado(Alimentacao, Agosto, 1200m)).Object);

        var resultado = await handler.HandleAsync(
            new ResumoDoMesQuery(Tenant, Agosto), CancellationToken.None);

        Assert.Single(resultado.Categorias);
        Assert.Equal(100m, resultado.ForaDoPlanejamento.Total);
        Assert.Equal(40m, resultado.ForaDoPlanejamento.PendenteDeClassificacao);

        var semPlano = Assert.Single(resultado.ForaDoPlanejamento.Categorias);
        Assert.Equal(Lazer, semPlano.CategoriaId);
        Assert.Equal(60m, semPlano.Realizado);

        Assert.Equal(1080m, resultado.TotalRealizado);
        Assert.Equal(1200m, resultado.TotalPlanejado);
    }

    /// <summary>
    /// Previsto é projeção, não gasto. Somá-lo faria a categoria aparecer estourada num mês em
    /// que a pessoa ainda não gastou nada.
    /// </summary>
    [Fact]
    public async Task Ocorrencia_ainda_prevista_nao_conta_como_realizado()
    {
        var handler = new ResumoDoMesQueryHandler(
            new RelatorioFalso(
                Linha("Aluguel", 2000m, Agosto, Agosto, Transporte, previsto: true),
                Linha("Combustível", 100m, Agosto, Agosto, Transporte)),
            Orcamentos(Orcado(Transporte, Agosto, 400m)).Object);

        var resultado = await handler.HandleAsync(
            new ResumoDoMesQuery(Tenant, Agosto), CancellationToken.None);

        var linha = Assert.Single(resultado.Categorias);

        Assert.Equal(100m, linha.Realizado);
        Assert.False(linha.Excedida);
    }

    [Fact]
    public async Task Mes_sem_lancamentos_e_sem_orcamento_devolve_resumo_vazio()
    {
        var handler = new ResumoDoMesQueryHandler(new RelatorioFalso(), Orcamentos().Object);

        var resultado = await handler.HandleAsync(
            new ResumoDoMesQuery(Tenant, Agosto), CancellationToken.None);

        Assert.Empty(resultado.Categorias);
        Assert.Empty(resultado.ForaDoPlanejamento.Categorias);
        Assert.Equal(0m, resultado.TotalRealizado);
        Assert.Equal(0m, resultado.Diferenca);
    }

    [Fact]
    public async Task Resumo_sem_tenant_devolve_vazio()
    {
        var relatorios = new Mock<IRelatorioReadRepository>();

        var handler = new ResumoDoMesQueryHandler(relatorios.Object, Orcamentos().Object);

        var resultado = await handler.HandleAsync(
            new ResumoDoMesQuery(Guid.Empty, Agosto, PorCaixa: true), CancellationToken.None);

        Assert.Empty(resultado.Categorias);
        Assert.True(resultado.VisaoPorCaixa);
        Assert.Equal(Agosto, resultado.Competencia);

        relatorios.Verify(
            r => r.TotaisPorCategoriaAsync(
                It.IsAny<PeriodoDoRelatorio>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// O planejado não muda com a visão — é um número só, definido por competência. O realizado
    /// muda. Na visão de caixa a comparação passa a responder "quanto do que planejei para
    /// agosto de fato saiu em agosto?".
    /// </summary>
    [Fact]
    public async Task Resumo_em_caixa_mantem_o_planejado_e_troca_so_o_realizado()
    {
        var relatorios = new RelatorioFalso(CompraParceladaEmJulho());
        var orcamentos = Orcamentos(Orcado(Alimentacao, Julho, 500m)).Object;

        var porCompetencia = await new ResumoDoMesQueryHandler(relatorios, orcamentos)
            .HandleAsync(new ResumoDoMesQuery(Tenant, Julho), CancellationToken.None);

        var porCaixa = await new ResumoDoMesQueryHandler(relatorios, orcamentos)
            .HandleAsync(new ResumoDoMesQuery(Tenant, Julho, PorCaixa: true), CancellationToken.None);

        Assert.Equal(500m, porCompetencia.TotalPlanejado);
        Assert.Equal(500m, porCaixa.TotalPlanejado);
        Assert.Equal(300m, porCompetencia.TotalRealizado);
        Assert.Equal(100m, porCaixa.TotalRealizado);
    }

    /// <summary>
    /// A consulta não recebe tenant: o recorte vem da RLS. O que o handler garante é não somar
    /// nada além do que veio — quem prova o isolamento de verdade é o teste de integração, que
    /// grava linhas de dois tenants e confere que uma delas não aparece.
    /// </summary>
    [Fact]
    public async Task Resumo_soma_apenas_o_que_o_repositorio_devolveu()
    {
        var handler = new ResumoDoMesQueryHandler(
            new RelatorioFalso(Linha("Mercado", 980m, Agosto, Agosto, Alimentacao, tenant: Tenant)),
            Orcamentos(Orcado(Alimentacao, Agosto, 1200m)).Object);

        var resultado = await handler.HandleAsync(
            new ResumoDoMesQuery(Tenant, Agosto), CancellationToken.None);

        Assert.Equal(980m, resultado.TotalRealizado);
        Assert.DoesNotContain(
            resultado.Categorias, linha => linha.CategoriaId == OutroTenant);
    }

    // ---------------------------------------------------------------- linha do resumo

    [Theory]
    [InlineData(1000, 1000, false, 0, 0)]
    [InlineData(1000, 999.99, false, 0.01, 0)]
    [InlineData(1000, 1000.01, true, 0, 0.01)]
    [InlineData(0, 50, true, 0, 50)]
    public void Linha_do_resumo_reparte_disponivel_e_excedente(
        decimal planejado, decimal realizado, bool excedida, decimal disponivel, decimal excedente)
    {
        var linha = new LinhaDoResumo(Alimentacao, planejado, realizado, 1);

        Assert.Equal(excedida, linha.Excedida);
        Assert.Equal(disponivel, linha.Disponivel);
        Assert.Equal(excedente, linha.Excedente);
    }

    [Fact]
    public void Lancamento_apurado_classifica_recorrente_e_avulso()
    {
        var avulso = Linha("Mercado", 10m, Agosto, Agosto, Alimentacao);

        var recorrente = Linha("Aluguel", 2000m, Agosto, Agosto, Alimentacao);
        recorrente.RecorrenciaId = Guid.CreateVersion7();

        Assert.Equal(TipoLancamento.Avulso, avulso.Tipo);
        Assert.Null(avulso.Parcelamento);
        Assert.Equal(TipoLancamento.Recorrente, recorrente.Tipo);
    }
}
