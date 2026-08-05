using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Services.V1.Alertas;
using Acme.Sistemas.Services.V1.Alertas.Command.DispensarAlerta;
using Acme.Sistemas.Services.V1.Classificacao.Command.AlterarRegra;
using Acme.Sistemas.Services.V1.Classificacao.Command.ClassificarLancamento;
using Acme.Sistemas.Services.V1.Classificacao.Command.RemoverRegra;
using Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;
using Acme.Sistemas.Services.V1.Compartilhamento.Command.RevogarCompartilhamento;
using Acme.Sistemas.Services.V1.Identidade.Command.Autenticar;
using Acme.Sistemas.Services.V1.Identidade.Command.CriarConta;
using Acme.Sistemas.Services.V1.Identidade.Command.ExcluirConta;
using Acme.Sistemas.Services.V1.Identidade.Command.RegistrarConsentimento;
using Acme.Sistemas.Services.V1.Identidade.Query.ConsentimentoVigente;
using Acme.Sistemas.Services.V1.Lancamentos.Command.AlterarLancamento;
using Acme.Sistemas.Services.V1.Lancamentos.Command.CriarCategoria;
using Acme.Sistemas.Services.V1.Lancamentos.Command.CriarLancamento;
using Acme.Sistemas.Services.V1.Lancamentos.Command.CriarRecorrencia;
using Acme.Sistemas.Services.V1.Lancamentos.Command.EncerrarRecorrencia;
using Acme.Sistemas.Services.V1.Lancamentos.Command.RemoverLancamento;
using Acme.Sistemas.Services.V1.Planejamento.Command.AlterarFonteRenda;
using Acme.Sistemas.Services.V1.Planejamento.Command.CadastrarFonteRenda;
using Acme.Sistemas.Services.V1.Planejamento.Command.CopiarOrcamento;
using Acme.Sistemas.Services.V1.Planejamento.Command.DefinirOrcamento;
using Acme.Sistemas.Services.V1.Planejamento.Command.RemoverFonteRenda;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

/// <summary>
/// Behaviors dos Commands: o que acontece depois que a gravação deu certo — e o que não pode
/// acontecer quando ela foi recusada.
/// </summary>
/// <remarks>
/// Os dois caminhos importam. Alguns destes behaviors disparam a reavaliação de alertas, e
/// dispará-la num Command recusado avaliaria um estado que nunca foi gravado: o alerta
/// apareceria ou sumiria por causa de uma operação que a pessoa não conseguiu concluir.
/// </remarks>
public sealed class BehaviorsDeComandoTests
{
    private static readonly Guid Tenant = Guid.CreateVersion7();
    private static readonly Guid Usuario = Guid.CreateVersion7();
    private static readonly Guid Alvo = Guid.CreateVersion7();
    private static readonly DateOnly Agosto = new(2026, 8, 1);
    private static readonly DateOnly Setembro = new(2026, 9, 1);
    private static readonly Error Recusa = new("recusado", "Não deu.");

    private static Mock<IAlertaService> Alertas()
    {
        var alertas = new Mock<IAlertaService>();

        alertas.Setup(a => a.AvaliarCompetenciaAsync(
                It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultadoDaAvaliacao.Nenhum);
        alertas.Setup(a => a.AvaliarCaixaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultadoDaAvaliacao.Nenhum);

        return alertas;
    }

    private static async Task<TResult> Executar<TRequest, TResult>(
        IPipelineBehavior<TRequest, TResult> behavior,
        TRequest request,
        TResult resultado)
        where TRequest : IRequest<TResult> =>
        await behavior.HandleAsync(request, () => Task.FromResult(resultado), CancellationToken.None);

    private static NullLogger<T> Log<T>() => NullLogger<T>.Instance;

    // ---------------------------------------------------------------- identidade

    [Fact]
    public async Task Criar_conta_registra_sucesso_e_recusa()
    {
        var behavior = new CriarContaCommandBehavior(Log<CriarContaCommandBehavior>());
        var comando = new CriarContaCommand("ana@teste.local", "Senha!Forte1", true, "1.0");

        Assert.True((await Executar(
            behavior, comando, CriarContaCommandResult.Criada(Usuario, Tenant))).Sucesso);

        Assert.False((await Executar(
            behavior, comando, CriarContaCommandResult.Recusada(Recusa))).Sucesso);
    }

    [Fact]
    public async Task Autenticar_registra_sucesso_e_recusa()
    {
        var behavior = new AutenticarCommandBehavior(Log<AutenticarCommandBehavior>());
        var comando = new AutenticarCommand("ana@teste.local", "Senha!Forte1");

        Assert.True((await Executar(
            behavior,
            comando,
            AutenticarCommandResult.Autenticado("token", DateTimeOffset.UtcNow))).Sucesso);

        Assert.False((await Executar(
            behavior, comando, AutenticarCommandResult.Recusado(Recusa))).Sucesso);
    }

    [Fact]
    public async Task Registrar_consentimento_registra_sucesso_e_recusa()
    {
        var behavior = new RegistrarConsentimentoCommandBehavior(
            Log<RegistrarConsentimentoCommandBehavior>());
        var comando = new RegistrarConsentimentoCommand(Usuario, "termos-de-uso", "1.0");

        Assert.True((await Executar(
            behavior, comando, RegistrarConsentimentoCommandResult.Registrado())).Sucesso);

        Assert.False((await Executar(
            behavior, comando, RegistrarConsentimentoCommandResult.Recusado(Recusa))).Sucesso);
    }

    [Fact]
    public async Task Excluir_conta_registra_sucesso_e_recusa()
    {
        var behavior = new ExcluirContaCommandBehavior(Log<ExcluirContaCommandBehavior>());
        var comando = new ExcluirContaCommand(Usuario, true);

        Assert.True((await Executar(
            behavior, comando, ExcluirContaCommandResult.Excluida())).Sucesso);

        Assert.False((await Executar(
            behavior, comando, ExcluirContaCommandResult.Recusada(Recusa))).Sucesso);
    }

    [Fact]
    public async Task Consentimento_vigente_repassa_os_dois_desfechos()
    {
        var behavior = new ConsentimentoVigenteQueryBehavior(
            Log<ConsentimentoVigenteQueryBehavior>());
        var consulta = new ConsentimentoVigenteQuery(Usuario, "1.0");

        Assert.True((await Executar(
            behavior, consulta, new ConsentimentoVigenteQueryResult(true, []))).Vigente);

        Assert.False((await Executar(
            behavior,
            consulta,
            new ConsentimentoVigenteQueryResult(false, ["termos-de-uso"]))).Vigente);
    }

    // ---------------------------------------------------------------- compartilhamento

    [Fact]
    public async Task Compartilhar_registra_sucesso_e_recusa()
    {
        var behavior = new CompartilharTenantCommandBehavior(
            Log<CompartilharTenantCommandBehavior>());
        var comando = new CompartilharTenantCommand(Usuario, Tenant, "bruno@teste.local", true, false);

        Assert.True((await Executar(
            behavior, comando, CompartilharTenantCommandResult.Concedido(Alvo))).Sucesso);

        Assert.False((await Executar(
            behavior, comando, CompartilharTenantCommandResult.Recusado(Recusa))).Sucesso);
    }

    [Fact]
    public async Task Revogar_registra_sucesso_e_recusa()
    {
        var behavior = new RevogarCompartilhamentoCommandBehavior(
            Log<RevogarCompartilhamentoCommandBehavior>());
        var comando = new RevogarCompartilhamentoCommand(Usuario, Tenant, Alvo);

        Assert.True((await Executar(
            behavior, comando, RevogarCompartilhamentoCommandResult.Revogado())).Sucesso);

        Assert.False((await Executar(
            behavior, comando, RevogarCompartilhamentoCommandResult.Recusado(Recusa))).Sucesso);
    }

    // ---------------------------------------------------------------- categorias e recorrência

    [Fact]
    public async Task Criar_categoria_registra_sucesso_e_recusa()
    {
        var behavior = new CriarCategoriaCommandBehavior(Log<CriarCategoriaCommandBehavior>());
        var comando = new CriarCategoriaCommand(Tenant, "Mercado");

        Assert.True((await Executar(
            behavior, comando, CriarCategoriaCommandResult.Criada(Alvo))).Sucesso);

        Assert.False((await Executar(
            behavior, comando, CriarCategoriaCommandResult.Recusada(Recusa))).Sucesso);
    }

    [Fact]
    public async Task Criar_recorrencia_registra_sucesso_e_recusa()
    {
        var behavior = new CriarRecorrenciaCommandBehavior(Log<CriarRecorrenciaCommandBehavior>());
        var comando = new CriarRecorrenciaCommand(
            Tenant, Usuario, "Aluguel", 2000m, 10, null, Agosto);

        Assert.True((await Executar(
            behavior, comando, CriarRecorrenciaCommandResult.Criada(Alvo, 12))).Sucesso);

        Assert.False((await Executar(
            behavior, comando, CriarRecorrenciaCommandResult.Recusada(Recusa))).Sucesso);
    }

    [Fact]
    public async Task Encerrar_recorrencia_registra_sucesso_e_recusa()
    {
        var behavior = new EncerrarRecorrenciaCommandBehavior(
            Log<EncerrarRecorrenciaCommandBehavior>());
        var comando = new EncerrarRecorrenciaCommand(Tenant, Alvo);

        Assert.True((await Executar(
            behavior, comando, EncerrarRecorrenciaCommandResult.Encerrada(Agosto))).Sucesso);

        Assert.False((await Executar(
            behavior, comando, EncerrarRecorrenciaCommandResult.Recusada(Recusa))).Sucesso);
    }

    // ---------------------------------------------------------------- planejamento

    [Fact]
    public async Task Cadastrar_renda_registra_sucesso_e_recusa()
    {
        var behavior = new CadastrarFonteRendaCommandBehavior(
            Log<CadastrarFonteRendaCommandBehavior>());
        var comando = new CadastrarFonteRendaCommand(Tenant, "Salário", 6000m, 5, Agosto);

        Assert.True((await Executar(
            behavior,
            comando,
            CadastrarFonteRendaCommandResult.Cadastrada(Alvo, Agosto, 6000m))).Sucesso);

        Assert.False((await Executar(
            behavior, comando, CadastrarFonteRendaCommandResult.Recusada(Recusa))).Sucesso);
    }

    [Fact]
    public async Task Alterar_renda_registra_sucesso_e_recusa()
    {
        var behavior = new AlterarFonteRendaCommandBehavior(Log<AlterarFonteRendaCommandBehavior>());
        var comando = new AlterarFonteRendaCommand(Tenant, Alvo, "Salário", 7000m, 5, Setembro);

        Assert.True((await Executar(
            behavior, comando, AlterarFonteRendaCommandResult.Alterada(Alvo, true, Setembro))).Sucesso);

        Assert.False((await Executar(
            behavior, comando, AlterarFonteRendaCommandResult.Recusada(Recusa))).Sucesso);
    }

    [Fact]
    public async Task Remover_renda_registra_sucesso_e_recusa()
    {
        var behavior = new RemoverFonteRendaCommandBehavior(Log<RemoverFonteRendaCommandBehavior>());
        var comando = new RemoverFonteRendaCommand(Tenant, Alvo);

        Assert.True((await Executar(
            behavior, comando, RemoverFonteRendaCommandResult.Removida())).Sucesso);

        Assert.False((await Executar(
            behavior, comando, RemoverFonteRendaCommandResult.Recusada(Recusa))).Sucesso);
    }

    /// <summary>
    /// O aviso de orçamento acima da renda é registrado só quando existe: contá-lo sempre
    /// esconderia, na operação, com que frequência o produto de fato avisa.
    /// </summary>
    [Fact]
    public async Task Definir_orcamento_registra_apenas_quando_ha_aviso()
    {
        var behavior = new DefinirOrcamentoCommandBehavior(Log<DefinirOrcamentoCommandBehavior>());
        var comando = new DefinirOrcamentoCommand(Tenant, Alvo, Agosto, 1200m);

        Assert.Null((await Executar(
            behavior,
            comando,
            DefinirOrcamentoCommandResult.Definido(Alvo, Agosto, 1200m, 6000m, null))).Aviso);

        Assert.NotNull((await Executar(
            behavior,
            comando,
            DefinirOrcamentoCommandResult.Definido(Alvo, Agosto, 6800m, 6000m, "estourou"))).Aviso);

        Assert.False((await Executar(
            behavior, comando, DefinirOrcamentoCommandResult.Recusado(Recusa))).Sucesso);
    }

    [Fact]
    public async Task Copiar_orcamento_registra_sucesso_confirmacao_e_recusa()
    {
        var behavior = new CopiarOrcamentoCommandBehavior(Log<CopiarOrcamentoCommandBehavior>());
        var comando = new CopiarOrcamentoCommand(Tenant, Agosto, Setembro);

        Assert.True((await Executar(
            behavior,
            comando,
            CopiarOrcamentoCommandResult.Copiado(Setembro, 3, 1600m, 6000m, null))).Sucesso);

        Assert.True((await Executar(
            behavior,
            comando,
            CopiarOrcamentoCommandResult.ConfirmacaoNecessaria(Setembro, 2, Recusa)))
            .RequerConfirmacao);

        Assert.False((await Executar(
            behavior, comando, CopiarOrcamentoCommandResult.Recusado(Recusa))).Sucesso);
    }

    // ---------------------------------------------------------------- classificação

    [Fact]
    public async Task Alterar_regra_registra_sucesso_e_recusa()
    {
        var behavior = new AlterarRegraCommandBehavior(Log<AlterarRegraCommandBehavior>());
        var comando = new AlterarRegraCommand(Tenant, Alvo, "IFOOD", Alvo);

        Assert.True((await Executar(
            behavior, comando, AlterarRegraCommandResult.Alterada("IFOOD"))).Sucesso);

        Assert.False((await Executar(
            behavior, comando, AlterarRegraCommandResult.Recusada(Recusa))).Sucesso);
    }

    [Fact]
    public async Task Remover_regra_registra_sucesso_e_recusa()
    {
        var behavior = new RemoverRegraCommandBehavior(Log<RemoverRegraCommandBehavior>());
        var comando = new RemoverRegraCommand(Tenant, Alvo);

        Assert.True((await Executar(
            behavior, comando, RemoverRegraCommandResult.Removida())).Sucesso);

        Assert.False((await Executar(
            behavior, comando, RemoverRegraCommandResult.Recusada(Recusa))).Sucesso);
    }

    [Fact]
    public async Task Dispensar_alerta_registra_sucesso_e_recusa()
    {
        var behavior = new DispensarAlertaCommandBehavior(Log<DispensarAlertaCommandBehavior>());
        var comando = new DispensarAlertaCommand(Tenant, Alvo);

        Assert.True((await Executar(
            behavior, comando, DispensarAlertaCommandResult.Dispensado())).Sucesso);

        Assert.False((await Executar(
            behavior, comando, DispensarAlertaCommandResult.Recusado(Recusa))).Sucesso);
    }

    // ---------------------------------------------------------------- gatilhos de alerta

    /// <summary>
    /// A parcela desloca dinheiro para meses à frente: o caixa muda mesmo quando a competência
    /// continua dentro do plano. Por isso os dois são avaliados.
    /// </summary>
    [Fact]
    public async Task Criar_lancamento_reavalia_competencia_do_request_e_o_caixa()
    {
        var alertas = Alertas();
        var behavior = new CriarLancamentoCommandBehavior(
            Log<CriarLancamentoCommandBehavior>(), alertas.Object);

        await Executar(
            behavior,
            new CriarLancamentoCommand(
                Tenant, Usuario, "Geladeira", 300m, Agosto, Agosto, null, 3),
            CriarLancamentoCommandResult.Criado([Alvo, Alvo, Alvo]));

        alertas.Verify(a => a.AvaliarCompetenciaAsync(Agosto, It.IsAny<CancellationToken>()), Times.Once);
        alertas.Verify(a => a.AvaliarCaixaAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Avaliar num Command recusado olharia um estado que nunca foi gravado: o alerta apareceria
    /// por causa de uma operação que a pessoa não conseguiu concluir.
    /// </summary>
    [Fact]
    public async Task Lancamento_recusado_nao_dispara_avaliacao()
    {
        var alertas = Alertas();
        var behavior = new CriarLancamentoCommandBehavior(
            Log<CriarLancamentoCommandBehavior>(), alertas.Object);

        var comando = new CriarLancamentoCommand(
            Tenant, Usuario, "Mercado", 100m, Agosto, Agosto, null);

        await Executar(behavior, comando, CriarLancamentoCommandResult.Recusado(Recusa));
        await Executar(behavior, comando, CriarLancamentoCommandResult.PossivelDuplicidade(2));

        alertas.Verify(
            a => a.AvaliarCompetenciaAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
        alertas.Verify(a => a.AvaliarCaixaAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Duas competências quando a alteração mudou o mês. Reapurar só a nova deixaria a antiga
    /// alertada para sempre, com um gasto que já não está lá.
    /// </summary>
    [Fact]
    public async Task Alterar_lancamento_reavalia_todas_as_competencias_afetadas()
    {
        var alertas = Alertas();
        var behavior = new AlterarLancamentoCommandBehavior(
            Log<AlterarLancamentoCommandBehavior>(), alertas.Object);

        await Executar(
            behavior,
            new AlterarLancamentoCommand(Tenant, Alvo, "Mercado", 100m, Setembro, Setembro, null),
            AlterarLancamentoCommandResult.Alterado([Agosto, Setembro], null));

        alertas.Verify(a => a.AvaliarCompetenciaAsync(Agosto, It.IsAny<CancellationToken>()), Times.Once);
        alertas.Verify(a => a.AvaliarCompetenciaAsync(Setembro, It.IsAny<CancellationToken>()), Times.Once);
        alertas.Verify(a => a.AvaliarCaixaAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Alteracao_recusada_nao_dispara_avaliacao()
    {
        var alertas = Alertas();
        var behavior = new AlterarLancamentoCommandBehavior(
            Log<AlterarLancamentoCommandBehavior>(), alertas.Object);

        await Executar(
            behavior,
            new AlterarLancamentoCommand(Tenant, Alvo, "Mercado", 100m, Agosto, Agosto, null),
            AlterarLancamentoCommandResult.Recusado(Recusa));

        alertas.Verify(
            a => a.AvaliarCompetenciaAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Remover é o caminho pelo qual um estouro se corrige.</summary>
    [Fact]
    public async Task Remover_lancamento_reavalia_a_competencia_removida()
    {
        var alertas = Alertas();
        var behavior = new RemoverLancamentoCommandBehavior(
            Log<RemoverLancamentoCommandBehavior>(), alertas.Object);

        await Executar(
            behavior,
            new RemoverLancamentoCommand(Tenant, Alvo),
            RemoverLancamentoCommandResult.Removido(Agosto, Alvo));

        alertas.Verify(a => a.AvaliarCompetenciaAsync(Agosto, It.IsAny<CancellationToken>()), Times.Once);
        alertas.Verify(a => a.AvaliarCaixaAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Remocao_recusada_nao_dispara_avaliacao()
    {
        var alertas = Alertas();
        var behavior = new RemoverLancamentoCommandBehavior(
            Log<RemoverLancamentoCommandBehavior>(), alertas.Object);

        await Executar(
            behavior,
            new RemoverLancamentoCommand(Tenant, Alvo),
            RemoverLancamentoCommandResult.Recusado(Recusa));

        alertas.Verify(a => a.AvaliarCaixaAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Classificar move valor entre categorias do mesmo mês — a competência muda, o caixa não.
    /// Reavaliar o caixa aqui seria trabalho sem efeito possível.
    /// </summary>
    [Fact]
    public async Task Classificar_reavalia_a_competencia_e_nao_o_caixa()
    {
        var alertas = Alertas();
        var behavior = new ClassificarLancamentoCommandBehavior(
            Log<ClassificarLancamentoCommandBehavior>(), alertas.Object);

        await Executar(
            behavior,
            new ClassificarLancamentoCommand(Tenant, Alvo, Alvo, CriarRegra: true),
            ClassificarLancamentoCommandResult.Classificado(Agosto, null, Alvo, "IFOOD"));

        alertas.Verify(a => a.AvaliarCompetenciaAsync(Agosto, It.IsAny<CancellationToken>()), Times.Once);
        alertas.Verify(a => a.AvaliarCaixaAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Classificacao_recusada_nao_dispara_avaliacao()
    {
        var alertas = Alertas();
        var behavior = new ClassificarLancamentoCommandBehavior(
            Log<ClassificarLancamentoCommandBehavior>(), alertas.Object);

        await Executar(
            behavior,
            new ClassificarLancamentoCommand(Tenant, Alvo, Alvo),
            ClassificarLancamentoCommandResult.Recusado(Recusa));

        alertas.Verify(
            a => a.AvaliarCompetenciaAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
