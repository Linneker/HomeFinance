using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Entities.Planejamento;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Services.V1.Planejamento.Command.AlterarFonteRenda;
using Acme.Sistemas.Services.V1.Planejamento.Command.CadastrarFonteRenda;
using Acme.Sistemas.Services.V1.Planejamento.Command.CopiarOrcamento;
using Acme.Sistemas.Services.V1.Planejamento.Command.DefinirOrcamento;
using Acme.Sistemas.Services.V1.Planejamento.Command.RemoverFonteRenda;
using Acme.Sistemas.Services.V1.Planejamento.Query.FontesRenda;
using Acme.Sistemas.Services.V1.Planejamento.Query.OrcamentoDoMes;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

/// <summary>
/// Renda declarada com vigência por mês e orçamento mensal por categoria.
/// </summary>
public sealed class PlanejamentoTests
{
    private static readonly Guid Tenant = Guid.CreateVersion7();
    private static readonly Guid Alimentacao = Guid.CreateVersion7();
    private static readonly Guid Transporte = Guid.CreateVersion7();

    private static readonly DateOnly Agosto = new(2026, 8, 1);
    private static readonly DateOnly Julho = new(2026, 7, 1);
    private static readonly DateOnly Setembro = new(2026, 9, 1);

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);
    }

    private static FonteRendaEntity Fonte(
        string nome,
        decimal valor,
        DateOnly inicio,
        DateOnly? fim = null,
        int dia = 5) => new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = Tenant,
            Nome = nome,
            Valor = valor,
            DiaRecebimento = dia,
            VigenciaInicio = inicio,
            VigenciaFim = fim,
            CriadoEm = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        };

    private static OrcamentoMensalEntity Orcado(Guid categoria, DateOnly mes, decimal valor) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Tenant,
        CategoriaId = categoria,
        Competencia = mes,
        ValorPlanejado = valor,
        CriadoEm = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
    };

    private static Mock<ICategoriaWriteRepository> Categorias(bool existe = true)
    {
        var categorias = new Mock<ICategoriaWriteRepository>();

        categorias.Setup(r => r.ExisteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existe);

        return categorias;
    }

    private static Mock<IFonteRendaWriteRepository> FontesEscrita(
        params FonteRendaEntity[] vigentes)
    {
        var fontes = new Mock<IFonteRendaWriteRepository>();

        fontes.Setup(r => r.ListarAsync(It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vigentes);

        return fontes;
    }

    // ---------------------------------------------------------------- calendário

    [Theory]
    [InlineData(2026, 8, 1)]
    [InlineData(2026, 8, 15)]
    [InlineData(2026, 8, 31)]
    public void Qualquer_dia_do_mes_reduz_ao_primeiro(int ano, int mes, int dia) =>
        Assert.Equal(
            new DateOnly(ano, mes, 1),
            CalendarioFinanceiro.PrimeiroDiaDoMes(new DateOnly(ano, mes, dia)));

    [Fact]
    public void Mes_anterior_a_marco_termina_no_ultimo_dia_de_fevereiro() =>
        Assert.Equal(
            new DateOnly(2026, 2, 28),
            CalendarioFinanceiro.UltimoDiaDoMesAnterior(new DateOnly(2026, 3, 20)));

    [Fact]
    public void Mes_anterior_a_janeiro_cruza_o_ano() =>
        Assert.Equal(
            new DateOnly(2025, 12, 31),
            CalendarioFinanceiro.UltimoDiaDoMesAnterior(new DateOnly(2026, 1, 1)));

    // ---------------------------------------------------------------- vigência

    [Fact]
    public void Fonte_vale_no_mes_em_que_a_vigencia_comeca() =>
        Assert.True(Fonte("Salário", 6000m, Agosto).VigenteEm(Agosto));

    [Fact]
    public void Fonte_nao_vale_em_mes_anterior_ao_inicio() =>
        Assert.False(Fonte("Salário", 6000m, Agosto).VigenteEm(Julho));

    [Fact]
    public void Fonte_sem_fim_vale_indefinidamente() =>
        Assert.True(Fonte("Salário", 6000m, Agosto).VigenteEm(new DateOnly(2030, 3, 1)));

    [Fact]
    public void Fonte_encerrada_nao_vale_depois_do_fim() =>
        Assert.False(
            Fonte("Salário", 6000m, Julho, new DateOnly(2026, 7, 31)).VigenteEm(Agosto));

    /// <summary>
    /// O dia informado não pode decidir a vigência: consultar "15/08" e "01/08" tem de dar o
    /// mesmo resultado, senão a tela mostraria renda diferente conforme o dia em que abrisse.
    /// </summary>
    [Fact]
    public void Vigencia_e_decidida_pelo_mes_e_nao_pelo_dia_consultado()
    {
        var fonte = Fonte("Salário", 6000m, Agosto);

        Assert.True(fonte.VigenteEm(new DateOnly(2026, 8, 15)));
        Assert.True(fonte.VigenteEm(new DateOnly(2026, 8, 31)));
    }

    [Fact]
    public void Recebimento_no_dia_31_cai_no_ultimo_dia_de_fevereiro()
    {
        var fonte = Fonte("Aluguel recebido", 1500m, new DateOnly(2026, 1, 1), dia: 31);

        Assert.Equal(new DateOnly(2026, 2, 28), fonte.RecebimentoEm(new DateOnly(2026, 2, 1)));
    }

    [Fact]
    public void Encerrar_antes_de_uma_vigencia_fecha_no_mes_anterior()
    {
        var fonte = Fonte("Salário", 6000m, Julho);

        fonte.EncerrarAntesDe(Agosto);

        Assert.Equal(new DateOnly(2026, 7, 31), fonte.VigenciaFim);
        Assert.True(fonte.Encerrada);
    }

    // ---------------------------------------------------------------- renda prevista

    [Fact]
    public void Renda_do_mes_e_a_soma_das_fontes_vigentes()
    {
        var renda = PrevisaoDeRenda.DoMes(
            Agosto,
            [Fonte("Salário", 6000m, Julho), Fonte("Freelance", 1200m, Agosto)]);

        Assert.Equal(7200m, renda);
    }

    [Fact]
    public void Renda_do_mes_ignora_fonte_ja_encerrada()
    {
        var renda = PrevisaoDeRenda.DoMes(
            Agosto,
            [
                Fonte("Salário antigo", 4000m, Janeiro(), new DateOnly(2026, 7, 31)),
                Fonte("Salário novo", 6000m, Agosto),
            ]);

        Assert.Equal(6000m, renda);
    }

    [Fact]
    public void Renda_e_zero_quando_nenhuma_fonte_vale_no_mes() =>
        Assert.Equal(0m, PrevisaoDeRenda.DoMes(Agosto, []));

    [Fact]
    public void Planejar_acima_da_renda_gera_aviso_com_o_excedente()
    {
        var aviso = PrevisaoDeRenda.Avaliar(6000m, 6800m);

        Assert.NotNull(aviso);
        Assert.Equal(800m, aviso.Excedente);
        Assert.Contains("6.800", aviso.Mensagem);
    }

    [Fact]
    public void Planejar_exatamente_a_renda_nao_gera_aviso() =>
        Assert.Null(PrevisaoDeRenda.Avaliar(6000m, 6000m));

    [Fact]
    public void Planejar_abaixo_da_renda_nao_gera_aviso() =>
        Assert.Null(PrevisaoDeRenda.Avaliar(6000m, 100m));

    /// <summary>
    /// Sem renda declarada o aviso apareceria já na primeira linha orçada, antes de existir o
    /// que comparar. Quem cobra a renda é o onboarding.
    /// </summary>
    [Fact]
    public void Sem_renda_declarada_nao_ha_aviso_de_excesso() =>
        Assert.Null(PrevisaoDeRenda.Avaliar(0m, 5000m));

    private static DateOnly Janeiro() => new(2026, 1, 1);

    // ---------------------------------------------------------------- cadastro de renda

    [Fact]
    public async Task Renda_cadastrada_grava_a_vigencia_no_primeiro_dia_do_mes()
    {
        FonteRendaEntity? gravada = null;
        var fontes = FontesEscrita();

        fontes.Setup(r => r.GravarAsync(It.IsAny<FonteRendaEntity>(), It.IsAny<CancellationToken>()))
            .Callback<FonteRendaEntity, CancellationToken>((fonte, _) => gravada = fonte)
            .Returns(Task.CompletedTask);

        var handler = new CadastrarFonteRendaCommandHandler(fontes.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new CadastrarFonteRendaCommand(Tenant, " Salário ", 6000m, 5, new DateOnly(2026, 8, 20)),
            CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.NotNull(gravada);
        Assert.Equal(Agosto, gravada.VigenciaInicio);
        Assert.Null(gravada.VigenciaFim);
        Assert.Equal("Salário", gravada.Nome);
        Assert.Equal(Agosto, resultado.VigenciaInicio);
    }

    /// <summary>
    /// Duas fontes é o cenário normal de quem tem emprego e um extra. A renda do mês é a soma,
    /// e é ela que o cadastro devolve para a tela não precisar de uma segunda chamada.
    /// </summary>
    [Fact]
    public async Task Duas_fontes_somam_na_renda_prevista_devolvida()
    {
        var fontes = FontesEscrita(
            Fonte("Salário", 6000m, Julho),
            Fonte("Freelance", 1200m, Agosto));

        var handler = new CadastrarFonteRendaCommandHandler(fontes.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new CadastrarFonteRendaCommand(Tenant, "Freelance", 1200m, 20, Agosto),
            CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(7200m, resultado.RendaPrevista);
    }

    [Theory]
    [InlineData("", 6000, 5, CadastrarFonteRendaCommandValidation.CodigoNomeObrigatorio)]
    [InlineData("   ", 6000, 5, CadastrarFonteRendaCommandValidation.CodigoNomeObrigatorio)]
    [InlineData("Salário", 0, 5, CadastrarFonteRendaCommandValidation.CodigoValorInvalido)]
    [InlineData("Salário", -1, 5, CadastrarFonteRendaCommandValidation.CodigoValorInvalido)]
    [InlineData("Salário", 6000, 0, CadastrarFonteRendaCommandValidation.CodigoDiaInvalido)]
    [InlineData("Salário", 6000, 32, CadastrarFonteRendaCommandValidation.CodigoDiaInvalido)]
    public async Task Cadastro_invalido_e_recusado(
        string nome, decimal valor, int dia, string codigo)
    {
        var fontes = FontesEscrita();
        var handler = new CadastrarFonteRendaCommandHandler(fontes.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new CadastrarFonteRendaCommand(Tenant, nome, valor, dia, Agosto),
            CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(codigo, resultado.Erros[0].Codigo);

        fontes.Verify(
            r => r.GravarAsync(It.IsAny<FonteRendaEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------------------------------------------------------------- alteração de renda

    /// <summary>
    /// O cenário central da spec: alterar a renda a partir de um mês não pode reescrever os
    /// meses já fechados.
    /// </summary>
    [Fact]
    public async Task Alteracao_a_partir_de_um_mes_preserva_os_meses_anteriores()
    {
        var atual = Fonte("Salário", 6000m, Julho);
        FonteRendaEntity? encerrada = null;
        FonteRendaEntity? nova = null;

        var fontes = FontesEscrita();
        fontes.Setup(r => r.ObterAsync(atual.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(atual);
        fontes.Setup(r => r.SucederAsync(
                It.IsAny<FonteRendaEntity>(),
                It.IsAny<FonteRendaEntity>(),
                It.IsAny<CancellationToken>()))
            .Callback<FonteRendaEntity, FonteRendaEntity, CancellationToken>(
                (fechada, aberta, _) => (encerrada, nova) = (fechada, aberta))
            .Returns(Task.CompletedTask);

        var handler = new AlterarFonteRendaCommandHandler(fontes.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new AlterarFonteRendaCommand(Tenant, atual.Id, "Salário", 7000m, 5, Setembro),
            CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.True(resultado.AbriuNovaVigencia);

        Assert.NotNull(encerrada);
        Assert.Equal(new DateOnly(2026, 8, 31), encerrada.VigenciaFim);
        Assert.Equal(6000m, encerrada.Valor);

        Assert.NotNull(nova);
        Assert.Equal(Setembro, nova.VigenciaInicio);
        Assert.Equal(7000m, nova.Valor);
        Assert.Null(nova.VigenciaFim);
        Assert.Equal(nova.Id, resultado.FonteId);
    }

    /// <summary>
    /// Agosto continua valendo 6.000 e setembro passa a valer 7.000 — as duas vigências não se
    /// sobrepõem em mês nenhum.
    /// </summary>
    [Fact]
    public async Task Renda_do_mes_anterior_permanece_apos_a_alteracao()
    {
        var atual = Fonte("Salário", 6000m, Julho);

        var fontes = FontesEscrita();
        fontes.Setup(r => r.ObterAsync(atual.Id, It.IsAny<CancellationToken>())).ReturnsAsync(atual);

        FonteRendaEntity? fechada = null;
        FonteRendaEntity? aberta = null;
        fontes.Setup(r => r.SucederAsync(
                It.IsAny<FonteRendaEntity>(),
                It.IsAny<FonteRendaEntity>(),
                It.IsAny<CancellationToken>()))
            .Callback<FonteRendaEntity, FonteRendaEntity, CancellationToken>(
                (a, b, _) => (fechada, aberta) = (a, b))
            .Returns(Task.CompletedTask);

        var handler = new AlterarFonteRendaCommandHandler(fontes.Object, new RelogioFixo());

        await handler.HandleAsync(
            new AlterarFonteRendaCommand(Tenant, atual.Id, "Salário", 7000m, 5, Setembro),
            CancellationToken.None);

        var historico = new[] { fechada!, aberta! };

        Assert.Equal(6000m, PrevisaoDeRenda.DoMes(Julho, historico));
        Assert.Equal(6000m, PrevisaoDeRenda.DoMes(Agosto, historico));
        Assert.Equal(7000m, PrevisaoDeRenda.DoMes(Setembro, historico));
    }

    /// <summary>
    /// Corrigir no mesmo mês em que a vigência começou não tem passado a proteger — abrir
    /// vigência nova aqui deixaria um registro de um mês que nunca existiu.
    /// </summary>
    [Fact]
    public async Task Correcao_no_proprio_mes_de_inicio_altera_a_linha_no_lugar()
    {
        var atual = Fonte("Salári", 5000m, Agosto);

        var fontes = FontesEscrita();
        fontes.Setup(r => r.ObterAsync(atual.Id, It.IsAny<CancellationToken>())).ReturnsAsync(atual);
        fontes.Setup(r => r.AlterarAsync(It.IsAny<FonteRendaEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new AlterarFonteRendaCommandHandler(fontes.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new AlterarFonteRendaCommand(Tenant, atual.Id, "Salário", 6000m, 10, Agosto),
            CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.False(resultado.AbriuNovaVigencia);
        Assert.Equal(atual.Id, resultado.FonteId);
        Assert.Equal("Salário", atual.Nome);
        Assert.Equal(6000m, atual.Valor);
        Assert.Equal(10, atual.DiaRecebimento);

        fontes.Verify(
            r => r.SucederAsync(
                It.IsAny<FonteRendaEntity>(),
                It.IsAny<FonteRendaEntity>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Alteracao_de_fonte_inexistente_e_recusada()
    {
        var fontes = FontesEscrita();
        fontes.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FonteRendaEntity?)null);

        var handler = new AlterarFonteRendaCommandHandler(fontes.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new AlterarFonteRendaCommand(
                Tenant, Guid.CreateVersion7(), "Salário", 6000m, 5, Agosto),
            CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            AlterarFonteRendaCommandHandler.CodigoFonteNaoEncontrada, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Vigencia_ja_encerrada_nao_pode_ser_alterada()
    {
        var antiga = Fonte("Salário", 4000m, Janeiro(), new DateOnly(2026, 7, 31));

        var fontes = FontesEscrita();
        fontes.Setup(r => r.ObterAsync(antiga.Id, It.IsAny<CancellationToken>())).ReturnsAsync(antiga);

        var handler = new AlterarFonteRendaCommandHandler(fontes.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new AlterarFonteRendaCommand(Tenant, antiga.Id, "Salário", 9000m, 5, Setembro),
            CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            AlterarFonteRendaCommandHandler.CodigoVigenciaEncerrada, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Alteracao_que_nao_encontra_a_linha_ao_gravar_e_recusada()
    {
        var atual = Fonte("Salário", 6000m, Agosto);

        var fontes = FontesEscrita();
        fontes.Setup(r => r.ObterAsync(atual.Id, It.IsAny<CancellationToken>())).ReturnsAsync(atual);
        fontes.Setup(r => r.AlterarAsync(It.IsAny<FonteRendaEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new AlterarFonteRendaCommandHandler(fontes.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new AlterarFonteRendaCommand(Tenant, atual.Id, "Salário", 6500m, 5, Agosto),
            CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            AlterarFonteRendaCommandHandler.CodigoFonteNaoEncontrada, resultado.Erros[0].Codigo);
    }

    [Theory]
    [InlineData("", 6000, 5, AlterarFonteRendaCommandValidation.CodigoNomeObrigatorio)]
    [InlineData("Salário", 0, 5, AlterarFonteRendaCommandValidation.CodigoValorInvalido)]
    [InlineData("Salário", 6000, 40, AlterarFonteRendaCommandValidation.CodigoDiaInvalido)]
    public async Task Alteracao_invalida_e_recusada(
        string nome, decimal valor, int dia, string codigo)
    {
        var fontes = FontesEscrita();
        var handler = new AlterarFonteRendaCommandHandler(fontes.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new AlterarFonteRendaCommand(Tenant, Guid.CreateVersion7(), nome, valor, dia, Agosto),
            CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(codigo, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Alteracao_sem_identificar_a_fonte_e_recusada()
    {
        var fontes = FontesEscrita();
        var handler = new AlterarFonteRendaCommandHandler(fontes.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new AlterarFonteRendaCommand(Tenant, Guid.Empty, "Salário", 6000m, 5, Agosto),
            CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            AlterarFonteRendaCommandValidation.CodigoFonteObrigatoria, resultado.Erros[0].Codigo);
    }

    // ---------------------------------------------------------------- remoção de renda

    [Fact]
    public async Task Fonte_removida_devolve_sucesso()
    {
        var fontes = FontesEscrita();
        fontes.Setup(r => r.RemoverAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new RemoverFonteRendaCommandHandler(fontes.Object);

        var resultado = await handler.HandleAsync(
            new RemoverFonteRendaCommand(Tenant, Guid.CreateVersion7()), CancellationToken.None);

        Assert.True(resultado.Sucesso);
    }

    [Fact]
    public async Task Remocao_de_fonte_inexistente_e_recusada()
    {
        var fontes = FontesEscrita();
        fontes.Setup(r => r.RemoverAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new RemoverFonteRendaCommandHandler(fontes.Object);

        var resultado = await handler.HandleAsync(
            new RemoverFonteRendaCommand(Tenant, Guid.CreateVersion7()), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            RemoverFonteRendaCommandHandler.CodigoFonteNaoEncontrada, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Remocao_sem_identificar_a_fonte_e_recusada()
    {
        var fontes = FontesEscrita();
        var handler = new RemoverFonteRendaCommandHandler(fontes.Object);

        var resultado = await handler.HandleAsync(
            new RemoverFonteRendaCommand(Tenant, Guid.Empty), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            RemoverFonteRendaCommandValidation.CodigoFonteObrigatoria, resultado.Erros[0].Codigo);

        fontes.Verify(
            r => r.RemoverAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------------------------------------------------------------- orçamento

    private static Mock<IOrcamentoWriteRepository> OrcamentosEscrita(
        params OrcamentoMensalEntity[] doMes)
    {
        var orcamentos = new Mock<IOrcamentoWriteRepository>();

        orcamentos.Setup(r => r.ListarDoMesAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(doMes);

        return orcamentos;
    }

    [Fact]
    public async Task Orcamento_definido_grava_no_primeiro_dia_do_mes()
    {
        OrcamentoMensalEntity? gravado = null;

        var orcamentos = OrcamentosEscrita(Orcado(Alimentacao, Agosto, 1200m));
        orcamentos.Setup(r => r.GravarAsync(
                It.IsAny<OrcamentoMensalEntity>(), It.IsAny<CancellationToken>()))
            .Callback<OrcamentoMensalEntity, CancellationToken>((item, _) => gravado = item)
            .Returns(Task.CompletedTask);

        var handler = new DefinirOrcamentoCommandHandler(
            orcamentos.Object,
            Categorias().Object,
            FontesEscrita(Fonte("Salário", 6000m, Agosto)).Object,
            new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new DefinirOrcamentoCommand(Tenant, Alimentacao, new DateOnly(2026, 8, 20), 1200m),
            CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.NotNull(gravado);
        Assert.Equal(Agosto, gravado.Competencia);
        Assert.Equal(1200m, resultado.TotalPlanejado);
        Assert.Equal(6000m, resultado.RendaPrevista);
        Assert.Null(resultado.Aviso);
    }

    /// <summary>Cenário literal da spec: 6.800 planejados contra 6.000 de renda.</summary>
    [Fact]
    public async Task Orcamento_acima_da_renda_avisa_mas_grava()
    {
        var orcamentos = OrcamentosEscrita(
            Orcado(Alimentacao, Agosto, 4000m),
            Orcado(Transporte, Agosto, 2800m));

        var handler = new DefinirOrcamentoCommandHandler(
            orcamentos.Object,
            Categorias().Object,
            FontesEscrita(Fonte("Salário", 6000m, Agosto)).Object,
            new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new DefinirOrcamentoCommand(Tenant, Transporte, Agosto, 2800m),
            CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.NotNull(resultado.Aviso);
        Assert.Equal(6800m, resultado.TotalPlanejado);
        Assert.Equal(6000m, resultado.RendaPrevista);

        orcamentos.Verify(
            r => r.GravarAsync(It.IsAny<OrcamentoMensalEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Valor_planejado_negativo_e_recusado()
    {
        var orcamentos = OrcamentosEscrita();

        var handler = new DefinirOrcamentoCommandHandler(
            orcamentos.Object, Categorias().Object, FontesEscrita().Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new DefinirOrcamentoCommand(Tenant, Alimentacao, Agosto, -1m),
            CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            DefinirOrcamentoCommandValidation.CodigoValorNegativo, resultado.Erros[0].Codigo);

        orcamentos.Verify(
            r => r.GravarAsync(It.IsAny<OrcamentoMensalEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Zero é decisão explícita de não gastar na categoria, e é diferente de não ter orçamento.
    /// </summary>
    [Fact]
    public async Task Valor_planejado_zero_e_aceito()
    {
        var orcamentos = OrcamentosEscrita(Orcado(Alimentacao, Agosto, 0m));

        var handler = new DefinirOrcamentoCommandHandler(
            orcamentos.Object, Categorias().Object, FontesEscrita().Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new DefinirOrcamentoCommand(Tenant, Alimentacao, Agosto, 0m), CancellationToken.None);

        Assert.True(resultado.Sucesso);
    }

    [Fact]
    public async Task Orcamento_em_categoria_inexistente_e_recusado()
    {
        var orcamentos = OrcamentosEscrita();

        var handler = new DefinirOrcamentoCommandHandler(
            orcamentos.Object,
            Categorias(existe: false).Object,
            FontesEscrita().Object,
            new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new DefinirOrcamentoCommand(Tenant, Alimentacao, Agosto, 500m), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            DefinirOrcamentoCommandHandler.CodigoCategoriaInexistente, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Orcamento_sem_categoria_e_recusado()
    {
        var handler = new DefinirOrcamentoCommandHandler(
            OrcamentosEscrita().Object,
            Categorias().Object,
            FontesEscrita().Object,
            new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new DefinirOrcamentoCommand(Tenant, Guid.Empty, Agosto, 500m), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            DefinirOrcamentoCommandValidation.CodigoCategoriaObrigatoria,
            resultado.Erros[0].Codigo);
    }

    // ---------------------------------------------------------------- cópia de orçamento

    private static Mock<IOrcamentoWriteRepository> ParaCopia(
        IReadOnlyList<OrcamentoMensalEntity> origem,
        IReadOnlyList<OrcamentoMensalEntity> destino)
    {
        var orcamentos = new Mock<IOrcamentoWriteRepository>();

        orcamentos.Setup(r => r.ListarDoMesAsync(Julho, It.IsAny<CancellationToken>()))
            .ReturnsAsync(origem);
        orcamentos.Setup(r => r.ListarDoMesAsync(Agosto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destino);
        orcamentos.Setup(r => r.SubstituirMesAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<IReadOnlyList<OrcamentoMensalEntity>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (DateOnly _, IReadOnlyList<OrcamentoMensalEntity> itens, CancellationToken _)
                    => itens.Count);

        return orcamentos;
    }

    [Fact]
    public async Task Copia_replica_todas_as_categorias_no_destino()
    {
        IReadOnlyList<OrcamentoMensalEntity>? copiadas = null;

        var orcamentos = ParaCopia(
            [Orcado(Alimentacao, Julho, 1200m), Orcado(Transporte, Julho, 400m)],
            []);

        orcamentos.Setup(r => r.SubstituirMesAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<IReadOnlyList<OrcamentoMensalEntity>>(),
                It.IsAny<CancellationToken>()))
            .Callback<DateOnly, IReadOnlyList<OrcamentoMensalEntity>, CancellationToken>(
                (_, itens, _) => copiadas = itens)
            .ReturnsAsync(2);

        var handler = new CopiarOrcamentoCommandHandler(
            orcamentos.Object,
            FontesEscrita(Fonte("Salário", 6000m, Agosto)).Object,
            new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new CopiarOrcamentoCommand(Tenant, Julho, Agosto), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(2, resultado.LinhasCopiadas);
        Assert.Equal(1600m, resultado.TotalPlanejado);

        Assert.NotNull(copiadas);
        Assert.All(copiadas, item => Assert.Equal(Agosto, item.Competencia));
        Assert.Contains(copiadas, item => item.CategoriaId == Alimentacao);
        Assert.Contains(copiadas, item => item.CategoriaId == Transporte);
    }

    /// <summary>
    /// Sem a confirmação, a cópia não pode apagar um mês que a pessoa já ajustou à mão.
    /// </summary>
    [Fact]
    public async Task Destino_com_valores_pede_confirmacao_e_nao_grava()
    {
        var orcamentos = ParaCopia(
            [Orcado(Alimentacao, Julho, 1200m)],
            [Orcado(Transporte, Agosto, 900m)]);

        var handler = new CopiarOrcamentoCommandHandler(
            orcamentos.Object, FontesEscrita().Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new CopiarOrcamentoCommand(Tenant, Julho, Agosto), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.True(resultado.RequerConfirmacao);
        Assert.Equal(1, resultado.LinhasNoDestino);
        Assert.Equal(
            CopiarOrcamentoCommandHandler.CodigoConfirmacaoNecessaria, resultado.Erros[0].Codigo);

        orcamentos.Verify(
            r => r.SubstituirMesAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<IReadOnlyList<OrcamentoMensalEntity>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Destino_com_valores_e_confirmacao_substitui()
    {
        var orcamentos = ParaCopia(
            [Orcado(Alimentacao, Julho, 1200m)],
            [Orcado(Transporte, Agosto, 900m)]);

        var handler = new CopiarOrcamentoCommandHandler(
            orcamentos.Object, FontesEscrita().Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new CopiarOrcamentoCommand(Tenant, Julho, Agosto, Confirmado: true),
            CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.False(resultado.RequerConfirmacao);
        Assert.Equal(1, resultado.LinhasCopiadas);
    }

    /// <summary>
    /// Copiar um mês vazio apagaria o destino sem colocar nada no lugar.
    /// </summary>
    [Fact]
    public async Task Origem_sem_orcamento_e_recusada()
    {
        var orcamentos = ParaCopia([], [Orcado(Transporte, Agosto, 900m)]);

        var handler = new CopiarOrcamentoCommandHandler(
            orcamentos.Object, FontesEscrita().Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new CopiarOrcamentoCommand(Tenant, Julho, Agosto), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(CopiarOrcamentoCommandHandler.CodigoOrigemVazia, resultado.Erros[0].Codigo);

        orcamentos.Verify(
            r => r.SubstituirMesAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<IReadOnlyList<OrcamentoMensalEntity>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Copiar_o_mes_para_ele_mesmo_e_recusado()
    {
        var handler = new CopiarOrcamentoCommandHandler(
            OrcamentosEscrita().Object, FontesEscrita().Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new CopiarOrcamentoCommand(Tenant, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)),
            CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            CopiarOrcamentoCommandValidation.CodigoMesesIguais, resultado.Erros[0].Codigo);
    }

    /// <summary>
    /// O plano de julho pode não caber em agosto se a renda mudou. O aviso tem de olhar a renda
    /// do destino, não a da origem.
    /// </summary>
    [Fact]
    public async Task Copia_avisa_quando_o_plano_herdado_nao_cabe_na_renda_do_destino()
    {
        var orcamentos = ParaCopia([Orcado(Alimentacao, Julho, 5000m)], []);

        var handler = new CopiarOrcamentoCommandHandler(
            orcamentos.Object,
            FontesEscrita(Fonte("Salário reduzido", 3000m, Agosto)).Object,
            new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new CopiarOrcamentoCommand(Tenant, Julho, Agosto), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.NotNull(resultado.Aviso);
        Assert.Equal(3000m, resultado.RendaPrevista);
    }

    // ---------------------------------------------------------------- consultas

    [Fact]
    public async Task Consulta_de_rendas_do_mes_devolve_a_renda_prevista_e_o_dia_de_recebimento()
    {
        var fontes = new Mock<IFonteRendaReadRepository>();
        fontes.Setup(r => r.ListarAsync(It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                Fonte("Salário", 6000m, Julho, dia: 31),
                Fonte("Freelance", 1200m, Agosto, dia: 20),
            ]);

        var handler = new FontesRendaQueryHandler(fontes.Object);

        var resultado = await handler.HandleAsync(
            new FontesRendaQuery(Tenant, new DateOnly(2026, 2, 10)), CancellationToken.None);

        Assert.Equal(2, resultado.Fontes.Count);
        Assert.Equal(new DateOnly(2026, 2, 28), resultado.Fontes[0].Recebimento);
    }

    [Fact]
    public async Task Consulta_sem_competencia_devolve_o_historico_sem_renda_do_mes()
    {
        var fontes = new Mock<IFonteRendaReadRepository>();
        fontes.Setup(r => r.ListarAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Fonte("Salário", 6000m, Julho, new DateOnly(2026, 7, 31))]);

        var handler = new FontesRendaQueryHandler(fontes.Object);

        var resultado = await handler.HandleAsync(
            new FontesRendaQuery(Tenant, null), CancellationToken.None);

        Assert.Single(resultado.Fontes);
        Assert.Null(resultado.Fontes[0].Recebimento);
        Assert.Equal(0m, resultado.RendaPrevista);
    }

    [Fact]
    public async Task Consulta_de_rendas_sem_tenant_devolve_vazio()
    {
        var fontes = new Mock<IFonteRendaReadRepository>();
        var handler = new FontesRendaQueryHandler(fontes.Object);

        var resultado = await handler.HandleAsync(
            new FontesRendaQuery(Guid.Empty, Agosto), CancellationToken.None);

        Assert.Empty(resultado.Fontes);
        Assert.Equal(0m, resultado.RendaPrevista);

        fontes.Verify(
            r => r.ListarAsync(It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consulta_do_orcamento_traz_total_renda_e_aviso()
    {
        var orcamentos = new Mock<IOrcamentoReadRepository>();
        orcamentos.Setup(r => r.ListarDoMesAsync(Agosto, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                Orcado(Alimentacao, Agosto, 4000m),
                Orcado(Transporte, Agosto, 2800m),
            ]);

        var fontes = new Mock<IFonteRendaReadRepository>();
        fontes.Setup(r => r.ListarAsync(It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Fonte("Salário", 6000m, Agosto)]);

        var handler = new OrcamentoDoMesQueryHandler(orcamentos.Object, fontes.Object);

        var resultado = await handler.HandleAsync(
            new OrcamentoDoMesQuery(Tenant, new DateOnly(2026, 8, 15)), CancellationToken.None);

        Assert.Equal(Agosto, resultado.Competencia);
        Assert.Equal(2, resultado.Categorias.Count);
        Assert.Equal(6800m, resultado.TotalPlanejado);
        Assert.Equal(6000m, resultado.RendaPrevista);
        Assert.NotNull(resultado.Aviso);
    }

    [Fact]
    public async Task Consulta_do_orcamento_sem_tenant_devolve_vazio()
    {
        var orcamentos = new Mock<IOrcamentoReadRepository>();
        var fontes = new Mock<IFonteRendaReadRepository>();

        var handler = new OrcamentoDoMesQueryHandler(orcamentos.Object, fontes.Object);

        var resultado = await handler.HandleAsync(
            new OrcamentoDoMesQuery(Guid.Empty, Agosto), CancellationToken.None);

        Assert.Empty(resultado.Categorias);
        Assert.Equal(Agosto, resultado.Competencia);
        Assert.Null(resultado.Aviso);

        orcamentos.Verify(
            r => r.ListarDoMesAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
