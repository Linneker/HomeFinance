using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Services.V1.Classificacao.Command.AlterarRegra;
using Acme.Sistemas.Services.V1.Classificacao.Command.ClassificarLancamento;
using Acme.Sistemas.Services.V1.Classificacao.Command.RemoverRegra;
using Acme.Sistemas.Services.V1.Classificacao.Query.Regras;
using Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;
using Acme.Sistemas.Services.V1.Compartilhamento.Query.ItensDoConsentimento;
using Acme.Sistemas.Services.V1.Compartilhamento.Query.TenantsDisponiveis;
using Acme.Sistemas.Services.V1.Identidade.Command.ExcluirConta;
using Acme.Sistemas.Services.V1.Identidade.Command.RegistrarConsentimento;
using Acme.Sistemas.Services.V1.Lancamentos.Command.AlterarLancamento;
using Acme.Sistemas.Services.V1.Lancamentos.Command.CriarCategoria;
using Acme.Sistemas.Services.V1.Lancamentos.Command.CriarLancamento;
using Acme.Sistemas.Services.V1.Lancamentos.Command.CriarRecorrencia;
using Acme.Sistemas.Services.V1.Lancamentos.Query.Categorias;
using Acme.Sistemas.Services.V1.Lancamentos.Query.Lancamentos;

namespace Acme.Sistemas.Services.UnitTest.Test;

/// <summary>
/// Recusas de entrada de todos os Commands e Queries.
/// </summary>
/// <remarks>
/// Estão juntas de propósito: são a fronteira do sistema e o único ponto onde uma regra
/// esquecida deixa passar dado inválido para o banco. Testá-las caso a caso dentro de cada
/// arquivo de handler espalharia a mesma pergunta — "o que este endpoint recusa?" — por doze
/// lugares.
/// </remarks>
public sealed class ValidacoesTests
{
    private static readonly Guid Tenant = Guid.CreateVersion7();
    private static readonly Guid Usuario = Guid.CreateVersion7();
    private static readonly DateOnly Hoje = new(2026, 8, 4);

    private static void Contem(IReadOnlyList<Error> erros, string codigo) =>
        Assert.Contains(erros, erro => erro.Codigo == codigo);

    // ---------------------------------------------------------------- categorias

    [Fact]
    public void Categoria_valida_passa() =>
        Assert.Empty(
            CriarCategoriaCommandValidation.Validar(new CriarCategoriaCommand(Tenant, "Mercado")));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Categoria_sem_nome_e_recusada(string nome) =>
        Contem(
            CriarCategoriaCommandValidation.Validar(new CriarCategoriaCommand(Tenant, nome)),
            CriarCategoriaCommandValidation.CodigoNomeObrigatorio);

    /// <summary>
    /// O limite existe para a categoria caber no rótulo da tela. Sem ele, um nome colado de
    /// engano viraria uma linha de gráfico ilegível para sempre.
    /// </summary>
    [Fact]
    public void Categoria_com_nome_longo_demais_e_recusada() =>
        Contem(
            CriarCategoriaCommandValidation.Validar(
                new CriarCategoriaCommand(
                    Tenant,
                    new string('x', CriarCategoriaCommandValidation.TamanhoMaximoDoNome + 1))),
            CriarCategoriaCommandValidation.CodigoNomeMuitoLongo);

    [Fact]
    public void Categoria_no_limite_exato_e_aceita() =>
        Assert.Empty(
            CriarCategoriaCommandValidation.Validar(
                new CriarCategoriaCommand(
                    Tenant,
                    new string('x', CriarCategoriaCommandValidation.TamanhoMaximoDoNome))));

    [Fact]
    public void Resultado_de_categoria_distingue_criada_de_recusada()
    {
        var criada = CriarCategoriaCommandResult.Criada(Tenant);
        var recusada = CriarCategoriaCommandResult.Recusada(new Error("x", "y"));

        Assert.True(criada.Sucesso);
        Assert.Equal(Tenant, criada.CategoriaId);
        Assert.False(recusada.Sucesso);
        Assert.Single(recusada.Erros);
    }

    // ---------------------------------------------------------------- lançamentos

    private static CriarLancamentoCommand Criacao(
        string descricao = "Mercado",
        decimal valor = 100m,
        DateOnly? competencia = null,
        DateOnly? caixa = null,
        int parcelas = 1) =>
        new(Tenant, Usuario, descricao, valor, competencia ?? Hoje, caixa ?? Hoje, null, parcelas);

    [Fact]
    public void Lancamento_valido_passa() => Assert.Empty(CriarLancamentoCommandValidation.Validar(Criacao()));

    [Theory]
    [InlineData("", CriarLancamentoCommandValidation.CodigoDescricaoObrigatoria)]
    [InlineData("   ", CriarLancamentoCommandValidation.CodigoDescricaoObrigatoria)]
    public void Lancamento_sem_descricao_e_recusado(string descricao, string codigo) =>
        Contem(CriarLancamentoCommandValidation.Validar(Criacao(descricao: descricao)), codigo);

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Lancamento_com_valor_nao_positivo_e_recusado(decimal valor) =>
        Contem(
            CriarLancamentoCommandValidation.Validar(Criacao(valor: valor)),
            CriarLancamentoCommandValidation.CodigoValorInvalido);

    /// <summary>
    /// As duas datas são exigidas em separado para a mensagem dizer qual está faltando.
    /// </summary>
    [Fact]
    public void Lancamento_sem_competencia_ou_sem_caixa_aponta_a_data_que_falta()
    {
        Contem(
            CriarLancamentoCommandValidation.Validar(
                new CriarLancamentoCommand(Tenant, Usuario, "Mercado", 10m, null, Hoje, null)),
            CriarLancamentoCommandValidation.CodigoCompetenciaObrigatoria);

        Contem(
            CriarLancamentoCommandValidation.Validar(
                new CriarLancamentoCommand(Tenant, Usuario, "Mercado", 10m, Hoje, null, null)),
            CriarLancamentoCommandValidation.CodigoCaixaObrigatoria);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(CriarLancamentoCommandValidation.MaximoDeParcelas + 1)]
    public void Numero_de_parcelas_fora_da_faixa_e_recusado(int parcelas) =>
        Contem(
            CriarLancamentoCommandValidation.Validar(Criacao(parcelas: parcelas)),
            CriarLancamentoCommandValidation.CodigoParcelasInvalidas);

    [Theory]
    [InlineData(1)]
    [InlineData(CriarLancamentoCommandValidation.MaximoDeParcelas)]
    public void Numero_de_parcelas_nos_extremos_da_faixa_e_aceito(int parcelas) =>
        Assert.Empty(CriarLancamentoCommandValidation.Validar(Criacao(parcelas: parcelas)));

    [Fact]
    public void Alteracao_de_lancamento_valida_passa() =>
        Assert.Empty(
            AlterarLancamentoCommandValidation.Validar(
                new AlterarLancamentoCommand(
                    Tenant, Guid.CreateVersion7(), "Mercado", 10m, Hoje, Hoje, null)));

    /// <summary>
    /// A alteração não afrouxa a exigência da dupla data: limpar uma delas tiraria o lançamento
    /// de uma das duas visões.
    /// </summary>
    [Fact]
    public void Alteracao_sem_identificador_datas_valor_ou_descricao_acumula_os_erros()
    {
        var erros = AlterarLancamentoCommandValidation.Validar(
            new AlterarLancamentoCommand(Tenant, Guid.Empty, " ", 0m, null, null, null));

        Contem(erros, AlterarLancamentoCommandValidation.CodigoLancamentoObrigatorio);
        Contem(erros, AlterarLancamentoCommandValidation.CodigoDescricaoObrigatoria);
        Contem(erros, AlterarLancamentoCommandValidation.CodigoValorInvalido);
        Contem(erros, AlterarLancamentoCommandValidation.CodigoCompetenciaObrigatoria);
        Contem(erros, AlterarLancamentoCommandValidation.CodigoCaixaObrigatoria);
        Assert.Equal(5, erros.Count);
    }

    // ---------------------------------------------------------------- recorrência

    private static CriarRecorrenciaCommand Recorrencia(
        string descricao = "Aluguel",
        decimal valor = 2000m,
        int dia = 10,
        DateOnly? inicio = null) =>
        new(Tenant, Usuario, descricao, valor, dia, null, inicio ?? Hoje);

    [Fact]
    public void Recorrencia_valida_passa() =>
        Assert.Empty(CriarRecorrenciaCommandValidation.Validar(Recorrencia()));

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Recorrencia_sem_descricao_e_recusada(string descricao) =>
        Contem(
            CriarRecorrenciaCommandValidation.Validar(Recorrencia(descricao: descricao)),
            CriarRecorrenciaCommandValidation.CodigoDescricaoObrigatoria);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Recorrencia_com_valor_nao_positivo_e_recusada(decimal valor) =>
        Contem(
            CriarRecorrenciaCommandValidation.Validar(Recorrencia(valor: valor)),
            CriarRecorrenciaCommandValidation.CodigoValorInvalido);

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Recorrencia_com_dia_fora_da_faixa_e_recusada(int dia) =>
        Contem(
            CriarRecorrenciaCommandValidation.Validar(Recorrencia(dia: dia)),
            CriarRecorrenciaCommandValidation.CodigoDiaInvalido);

    /// <summary>
    /// 29, 30 e 31 são aceitos: quem paga no último dia do mês precisa poder dizer isso. Quem
    /// recua para o fim do mês curto é a materialização, não a validação.
    /// </summary>
    [Theory]
    [InlineData(29)]
    [InlineData(30)]
    [InlineData(31)]
    public void Recorrencia_no_fim_do_mes_e_aceita(int dia) =>
        Assert.Empty(CriarRecorrenciaCommandValidation.Validar(Recorrencia(dia: dia)));

    [Fact]
    public void Recorrencia_sem_inicio_e_recusada() =>
        Contem(
            CriarRecorrenciaCommandValidation.Validar(
                new CriarRecorrenciaCommand(Tenant, Usuario, "Aluguel", 2000m, 10, null, null)),
            CriarRecorrenciaCommandValidation.CodigoInicioObrigatorio);

    // ---------------------------------------------------------------- classificação

    [Fact]
    public void Classificacao_valida_passa() =>
        Assert.Empty(
            ClassificarLancamentoCommandValidation.Validar(
                new ClassificarLancamentoCommand(
                    Tenant, Guid.CreateVersion7(), Guid.CreateVersion7())));

    /// <summary>
    /// Classificar exige categoria. Limpar a classificação é alteração de lançamento — juntar
    /// as duas coisas aqui esconderia a intenção de quem chama.
    /// </summary>
    [Fact]
    public void Classificacao_sem_lancamento_ou_sem_categoria_e_recusada()
    {
        var erros = ClassificarLancamentoCommandValidation.Validar(
            new ClassificarLancamentoCommand(Tenant, Guid.Empty, Guid.Empty));

        Contem(erros, ClassificarLancamentoCommandValidation.CodigoLancamentoObrigatorio);
        Contem(erros, ClassificarLancamentoCommandValidation.CodigoCategoriaObrigatoria);
    }

    [Fact]
    public void Resultado_de_remocao_de_regra_distingue_removida_de_recusada()
    {
        Assert.True(RemoverRegraCommandResult.Removida().Sucesso);
        Assert.False(RemoverRegraCommandResult.Recusada(new Error("x", "y")).Sucesso);
    }

    [Fact]
    public void Alteracao_de_regra_valida_passa() =>
        Assert.Empty(
            AlterarRegraCommandValidation.Validar(
                new AlterarRegraCommand(
                    Tenant, Guid.CreateVersion7(), "IFOOD", Guid.CreateVersion7())));

    [Fact]
    public void Alteracao_de_regra_sem_identificador_e_recusada() =>
        Contem(
            AlterarRegraCommandValidation.Validar(
                new AlterarRegraCommand(Tenant, Guid.Empty, "IFOOD", Guid.CreateVersion7())),
            AlterarRegraCommandValidation.CodigoRegraObrigatoria);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Alteracao_de_regra_sem_padrao_e_recusada(string padrao) =>
        Contem(
            AlterarRegraCommandValidation.Validar(
                new AlterarRegraCommand(
                    Tenant, Guid.CreateVersion7(), padrao, Guid.CreateVersion7())),
            AlterarRegraCommandValidation.CodigoPadraoObrigatorio);

    /// <summary>
    /// Padrão de uma ou duas letras casaria com quase toda descrição e classificaria o tenant
    /// inteiro de uma vez.
    /// </summary>
    [Theory]
    [InlineData("A")]
    [InlineData("AB")]
    [InlineData(" AB ")]
    public void Alteracao_de_regra_com_padrao_curto_demais_e_recusada(string padrao) =>
        Contem(
            AlterarRegraCommandValidation.Validar(
                new AlterarRegraCommand(
                    Tenant, Guid.CreateVersion7(), padrao, Guid.CreateVersion7())),
            AlterarRegraCommandValidation.CodigoPadraoMuitoCurto);

    [Fact]
    public void Alteracao_de_regra_sem_categoria_e_recusada() =>
        Contem(
            AlterarRegraCommandValidation.Validar(
                new AlterarRegraCommand(Tenant, Guid.CreateVersion7(), "IFOOD", Guid.Empty)),
            AlterarRegraCommandValidation.CodigoCategoriaObrigatoria);

    // ---------------------------------------------------------------- compartilhamento

    [Fact]
    public void Compartilhamento_valido_passa() =>
        Assert.Empty(
            CompartilharTenantCommandValidation.Validar(
                new CompartilharTenantCommand(Usuario, null, "bruno@teste.local", true, false)));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Compartilhamento_sem_email_e_recusado(string email) =>
        Contem(
            CompartilharTenantCommandValidation.Validar(
                new CompartilharTenantCommand(Usuario, null, email, true, false)),
            CompartilharTenantCommandValidation.CodigoEmailObrigatorio);

    /// <summary>
    /// Sem o aceite explícito não há como sustentar que a pessoa soube o que ficaria visível.
    /// </summary>
    [Fact]
    public void Compartilhamento_sem_consentimento_confirmado_e_recusado() =>
        Contem(
            CompartilharTenantCommandValidation.Validar(
                new CompartilharTenantCommand(Usuario, null, "bruno@teste.local", false, false)),
            CompartilharTenantCommandValidation.CodigoConsentimentoNaoConfirmado);

    [Fact]
    public void Consulta_de_tenants_sem_titular_e_recusada() =>
        Contem(
            TenantsDisponiveisQueryValidation.Validar(new TenantsDisponiveisQuery(Guid.Empty)),
            TenantsDisponiveisQueryValidation.CodigoUsuarioObrigatorio);

    [Fact]
    public void Consulta_de_tenants_com_titular_passa() =>
        Assert.Empty(TenantsDisponiveisQueryValidation.Validar(new TenantsDisponiveisQuery(Usuario)));

    [Fact]
    public async Task Catalogo_do_consentimento_lista_os_itens_e_marca_o_sensivel()
    {
        var resultado = await new ItensDoConsentimentoQueryHandler().HandleAsync(
            new ItensDoConsentimentoQuery(Usuario), CancellationToken.None);

        Assert.NotEmpty(resultado.Itens);
        Assert.Contains(resultado.Itens, item => item.Chave == CatalogoDeCompartilhamento.ChaveScr);
        Assert.All(resultado.Itens, item => Assert.Equal(!item.Sensivel, item.MarcadoPorPadrao));
    }

    [Fact]
    public async Task Catalogo_do_consentimento_sem_titular_devolve_vazio()
    {
        var resultado = await new ItensDoConsentimentoQueryHandler().HandleAsync(
            new ItensDoConsentimentoQuery(Guid.Empty), CancellationToken.None);

        Assert.Empty(resultado.Itens);
    }

    // ---------------------------------------------------------------- identidade

    [Theory]
    [InlineData(DocumentoConsentimento.TermosDeUso)]
    [InlineData(DocumentoConsentimento.PoliticaDePrivacidade)]
    public void Consentimento_de_documento_conhecido_passa(string documento) =>
        Assert.Empty(
            RegistrarConsentimentoCommandValidation.Validar(
                new RegistrarConsentimentoCommand(Usuario, documento, "2026-01")));

    /// <summary>
    /// Aceitar documento arbitrário permitiria registrar consentimento de algo que não existe,
    /// e a prova do aceite deixaria de significar qualquer coisa.
    /// </summary>
    [Fact]
    public void Consentimento_de_documento_desconhecido_e_recusado() =>
        Contem(
            RegistrarConsentimentoCommandValidation.Validar(
                new RegistrarConsentimentoCommand(Usuario, "contrato-inventado", "2026-01")),
            RegistrarConsentimentoCommandValidation.CodigoDocumentoDesconhecido);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Consentimento_sem_versao_e_recusado(string versao) =>
        Contem(
            RegistrarConsentimentoCommandValidation.Validar(
                new RegistrarConsentimentoCommand(
                    Usuario, DocumentoConsentimento.TermosDeUso, versao)),
            RegistrarConsentimentoCommandValidation.CodigoVersaoObrigatoria);

    [Fact]
    public void Exclusao_confirmada_pelo_titular_passa() =>
        Assert.Empty(ExcluirContaCommandValidation.Validar(new ExcluirContaCommand(Usuario, true)));

    [Fact]
    public void Exclusao_sem_titular_ou_sem_confirmacao_e_recusada()
    {
        var erros = ExcluirContaCommandValidation.Validar(new ExcluirContaCommand(Guid.Empty, false));

        Contem(erros, ExcluirContaCommandValidation.CodigoUsuarioObrigatorio);
        Contem(erros, ExcluirContaCommandValidation.CodigoConfirmacaoAusente);
    }

    // ---------------------------------------------------------------- consultas

    [Fact]
    public void Consulta_de_regras_sem_tenant_e_recusada() =>
        Contem(
            RegrasQueryValidation.Validar(new RegrasQuery(Guid.Empty)),
            RegrasQueryValidation.CodigoTenantObrigatorio);

    [Fact]
    public void Consulta_de_regras_com_tenant_passa() =>
        Assert.Empty(RegrasQueryValidation.Validar(new RegrasQuery(Tenant)));

    [Fact]
    public void Consulta_de_categorias_sem_tenant_e_recusada() =>
        Contem(
            CategoriasQueryValidation.Validar(new CategoriasQuery(Guid.Empty)),
            CategoriasQueryValidation.CodigoTenantObrigatorio);

    [Fact]
    public void Consulta_de_categorias_com_tenant_passa() =>
        Assert.Empty(CategoriasQueryValidation.Validar(new CategoriasQuery(Tenant)));

    /// <summary>
    /// Listagem sem recorte percorreria o histórico inteiro do cliente e deixaria de usar o
    /// índice por tenant e data.
    /// </summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Consulta_de_lancamentos_sem_periodo_e_recusada(bool semDe, bool semAte) =>
        Contem(
            LancamentosQueryValidation.Validar(
                new LancamentosQuery(Tenant, semDe ? null : Hoje, semAte ? null : Hoje)),
            LancamentosQueryValidation.CodigoPeriodoObrigatorio);

    [Fact]
    public void Consulta_de_lancamentos_com_periodo_invertido_e_recusada() =>
        Contem(
            LancamentosQueryValidation.Validar(
                new LancamentosQuery(Tenant, new DateOnly(2026, 8, 31), new DateOnly(2026, 8, 1))),
            LancamentosQueryValidation.CodigoPeriodoInvertido);

    [Fact]
    public void Consulta_de_lancamentos_com_periodo_valido_passa() =>
        Assert.Empty(
            LancamentosQueryValidation.Validar(
                new LancamentosQuery(Tenant, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31))));

    [Fact]
    public void Resultado_de_lancamentos_soma_o_total_e_declara_a_visao()
    {
        var resultado = new LancamentosQueryResult(
            [
                new LancamentoListado(
                    Guid.CreateVersion7(), "A", 10.50m, Hoje, Hoje, null, true,
                    Domain.Enums.TipoLancamento.Avulso, null, false),
                new LancamentoListado(
                    Guid.CreateVersion7(), "B", 4.50m, Hoje, Hoje, null, true,
                    Domain.Enums.TipoLancamento.Avulso, null, false),
            ],
            VisaoPorCaixa: true);

        Assert.Equal(15m, resultado.Total);
        Assert.True(resultado.VisaoPorCaixa);
        Assert.Empty(resultado.Erros);
    }

    [Fact]
    public void Resultado_de_lancamentos_recusado_vem_vazio_por_competencia()
    {
        var resultado = LancamentosQueryResult.Recusada(new Error("x", "y"));

        Assert.Empty(resultado.Lancamentos);
        Assert.False(resultado.VisaoPorCaixa);
        Assert.Equal(0m, resultado.Total);
        Assert.Single(resultado.Erros);
    }

    // ---------------------------------------------------------------- estados de entidade

    [Fact]
    public void Usuario_sem_data_de_exclusao_esta_ativo()
    {
        var usuario = new UsuarioEntity { Email = "ana@teste.local", SenhaHash = "x" };

        Assert.True(usuario.Ativo);

        usuario.ExcluidoEm = DateTimeOffset.UtcNow;

        Assert.False(usuario.Ativo);
    }

    [Fact]
    public void Compartilhamento_sem_revogacao_esta_vigente()
    {
        var compartilhamento = new TenantCompartilhamentoEntity();

        Assert.True(compartilhamento.Vigente);

        compartilhamento.RevogadoEm = DateTimeOffset.UtcNow;

        Assert.False(compartilhamento.Vigente);
    }

    [Fact]
    public void Recorrencia_sem_encerramento_esta_ativa()
    {
        var recorrencia = new RecorrenciaEntity { Descricao = "Aluguel" };

        Assert.True(recorrencia.Ativa);

        recorrencia.EncerradaEm = DateTimeOffset.UtcNow;

        Assert.False(recorrencia.Ativa);
    }

    /// <summary>
    /// A participação nasce integral nesta fase. O rateio entre pessoas entra depois, e é a
    /// existência da tabela desde já que o torna possível sem reescrever relatório nenhum.
    /// </summary>
    /// <remarks>
    /// A constante é lida por reflexão, e não escrita direto: <c>const decimal</c> é gravado
    /// pelo compilador como campo estático com atributo, e todo uso comum é substituído pelo
    /// literal — o campo em si nunca seria lido, nem por este teste.
    /// </remarks>
    [Fact]
    public void Participacao_integral_e_cem_por_cento()
    {
        var campo = typeof(LancamentoParticipacaoEntity)
            .GetField(nameof(LancamentoParticipacaoEntity.PercentualIntegral));

        Assert.Equal(100m, campo!.GetValue(null));

        var participacao = new LancamentoParticipacaoEntity
        {
            Percentual = LancamentoParticipacaoEntity.PercentualIntegral,
        };

        Assert.Equal(100m, participacao.Percentual);
    }

    /// <summary>
    /// Início já passado no mês corrente empurra a primeira ocorrência para o mês seguinte —
    /// sem isso, uma recorrência criada no dia 20 para o dia 5 geraria uma cobrança retroativa.
    /// </summary>
    [Fact]
    public void Primeira_ocorrencia_de_dia_ja_passado_cai_no_mes_seguinte() =>
        Assert.Equal(
            new DateOnly(2026, 9, 5),
            CalendarioFinanceiro.PrimeiraOcorrencia(new DateOnly(2026, 8, 20), 5));

    [Fact]
    public void Primeira_ocorrencia_de_dia_ainda_por_vir_fica_no_mes_do_inicio() =>
        Assert.Equal(
            new DateOnly(2026, 8, 25),
            CalendarioFinanceiro.PrimeiraOcorrencia(new DateOnly(2026, 8, 20), 25));

    [Fact]
    public void Primeira_ocorrencia_de_dia_31_recua_ao_ultimo_dia_do_mes_curto() =>
        Assert.Equal(
            new DateOnly(2026, 2, 28),
            CalendarioFinanceiro.PrimeiraOcorrencia(new DateOnly(2026, 2, 1), 31));
}
