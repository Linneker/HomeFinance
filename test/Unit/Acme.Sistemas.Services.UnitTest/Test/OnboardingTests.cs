using System.Reflection;
using Acme.Sistemas.Core;
using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Entities.Onboarding;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Services;
using Acme.Sistemas.Services.V1.Lancamentos.Command.CriarLancamento;
using Acme.Sistemas.Services.V1.Lancamentos.Query.Lancamentos;
using Acme.Sistemas.Services.V1.Onboarding;
using Acme.Sistemas.Services.V1.Onboarding.Query.ProgressoOnboarding;
using Acme.Sistemas.Services.V1.Planejamento.Command.CadastrarFonteRenda;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

/// <summary>
/// Onboarding progressivo: o que ele bloqueia, o que não bloqueia e como a barra avança.
/// </summary>
public sealed class OnboardingTests
{
    private static readonly Guid Tenant = Guid.CreateVersion7();
    private static readonly Guid Ana = Guid.CreateVersion7();
    private static readonly DateOnly Agosto = new(2026, 8, 1);
    private static readonly DateTimeOffset Agora = new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Agora;
    }

    private sealed class ContextoDeTeste : IContextoRequisicao
    {
        public Guid? UsuarioId { get; set; } = Ana;

        public Guid? TenantId { get; set; } = Tenant;

        public bool SomenteLeitura { get; set; }
    }

    private static OnboardingRequisitoEntity Requisito(
        string codigo, int ordem, bool ativo, bool bloqueante) => new()
        {
            Codigo = codigo,
            Descricao = $"Passo {codigo}",
            Ordem = ordem,
            Ativo = ativo,
            Bloqueante = bloqueante,
        };

    private static OnboardingEstadoEntity Cumprido(string codigo) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Tenant,
        RequisitoCodigo = codigo,
        CumpridoEm = Agora,
    };

    /// <summary>Catálogo real do produto: renda ativa e bloqueante, os documentais anunciados.</summary>
    private static OnboardingRequisitoEntity[] CatalogoDoProduto() =>
    [
        Requisito(OnboardingRequisitoEntity.CodigoRendaDeclarada, 1, ativo: true, bloqueante: true),
        Requisito("registrato_3_meses", 2, ativo: false, bloqueante: false),
        Requisito("faturas_3_meses", 3, ativo: false, bloqueante: false),
    ];

    private static Mock<IOnboardingWriteRepository> Repositorio(
        IReadOnlyList<OnboardingRequisitoEntity> catalogo,
        params OnboardingEstadoEntity[] estados)
    {
        var repositorio = new Mock<IOnboardingWriteRepository>();

        repositorio.Setup(r => r.CatalogoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalogo);
        repositorio.Setup(r => r.EstadoDoTenantAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(estados);

        return repositorio;
    }

    // ---------------------------------------------------------------- progresso

    [Fact]
    public void Sem_nada_cumprido_o_progresso_e_zero_e_o_bloqueio_existe()
    {
        var progresso = ProgressoDoOnboarding.Montar(CatalogoDoProduto(), []);

        Assert.Equal(0, progresso.PercentualConcluido);
        Assert.False(progresso.Liberado);
        Assert.False(progresso.Concluido);
        Assert.Equal(
            OnboardingRequisitoEntity.CodigoRendaDeclarada, progresso.Bloqueios[0].Codigo);
    }

    /// <summary>
    /// As etapas futuras ficam fora do percentual. Incluí-las travaria a barra num teto que a
    /// pessoa não tem como alcançar — ela faria tudo o que o produto pede e ainda veria 33%.
    /// </summary>
    [Fact]
    public void Etapas_futuras_nao_entram_no_percentual()
    {
        var progresso = ProgressoDoOnboarding.Montar(
            CatalogoDoProduto(), [Cumprido(OnboardingRequisitoEntity.CodigoRendaDeclarada)]);

        Assert.Equal(100, progresso.PercentualConcluido);
        Assert.True(progresso.Concluido);
        Assert.True(progresso.Liberado);
    }

    /// <summary>
    /// Requisito declarado e ainda não ativo aparece na barra como etapa futura, e não bloqueia.
    /// </summary>
    [Fact]
    public void Requisito_declarado_e_inativo_aparece_mas_nao_bloqueia()
    {
        var progresso = ProgressoDoOnboarding.Montar(
            [Requisito("documentos", 1, ativo: false, bloqueante: true)], []);

        var passo = Assert.Single(progresso.Passos);

        Assert.True(passo.Futuro);
        Assert.False(passo.Bloqueante);
        Assert.True(progresso.Liberado);
        Assert.Empty(progresso.Pendentes);
    }

    [Fact]
    public void Progresso_parcial_conta_apenas_os_ativos()
    {
        var progresso = ProgressoDoOnboarding.Montar(
            [
                Requisito("a", 1, ativo: true, bloqueante: true),
                Requisito("b", 2, ativo: true, bloqueante: false),
                Requisito("c", 3, ativo: false, bloqueante: false),
            ],
            [Cumprido("a")]);

        Assert.Equal(50, progresso.PercentualConcluido);
        Assert.True(progresso.Liberado);
        Assert.False(progresso.Concluido);
        Assert.Equal("b", Assert.Single(progresso.Pendentes).Codigo);
    }

    [Fact]
    public void Catalogo_vazio_considera_o_onboarding_concluido() =>
        Assert.Equal(100, ProgressoDoOnboarding.Montar([], []).PercentualConcluido);

    // ---------------------------------------------------------------- serviço

    [Fact]
    public async Task Sem_tenant_selecionado_o_progresso_nao_consulta_estado()
    {
        var repositorio = Repositorio(CatalogoDoProduto());

        var progresso = await new OnboardingService(
                repositorio.Object,
                new ContextoDeTeste { TenantId = null },
                new RelogioFixo())
            .ProgressoAsync(CancellationToken.None);

        Assert.Equal(0, progresso.PercentualConcluido);

        repositorio.Verify(
            r => r.EstadoDoTenantAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cumprimento_grava_o_estado_com_a_data()
    {
        OnboardingEstadoEntity? gravado = null;
        var repositorio = Repositorio(CatalogoDoProduto());

        repositorio.Setup(r => r.MarcarCumpridoAsync(
                It.IsAny<OnboardingEstadoEntity>(), It.IsAny<CancellationToken>()))
            .Callback<OnboardingEstadoEntity, CancellationToken>((estado, _) => gravado = estado)
            .Returns(Task.CompletedTask);

        await new OnboardingService(repositorio.Object, new ContextoDeTeste(), new RelogioFixo())
            .CumprirAsync(OnboardingRequisitoEntity.CodigoRendaDeclarada, CancellationToken.None);

        Assert.NotNull(gravado);
        Assert.Equal(Tenant, gravado.TenantId);
        Assert.Equal(Agora, gravado.CumpridoEm);
        Assert.True(gravado.Cumprido);
    }

    /// <summary>
    /// O onboarding é de quem é dono do espaço. Gravar num tenant de terceiro registraria ali um
    /// passo que quem está olhando não deu.
    /// </summary>
    [Fact]
    public async Task Cumprimento_em_tenant_de_terceiro_nao_grava()
    {
        var repositorio = Repositorio(CatalogoDoProduto());

        await new OnboardingService(
                repositorio.Object,
                new ContextoDeTeste { SomenteLeitura = true },
                new RelogioFixo())
            .CumprirAsync(OnboardingRequisitoEntity.CodigoRendaDeclarada, CancellationToken.None);

        repositorio.Verify(
            r => r.MarcarCumpridoAsync(
                It.IsAny<OnboardingEstadoEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------------------------------------------------------------- behavior

    private static OnboardingBehavior<CriarLancamentoCommand, CriarLancamentoCommandResult> Behavior(
        IOnboardingService onboarding,
        IContextoRequisicao? contexto = null) =>
        new(onboarding,
            contexto ?? new ContextoDeTeste(),
            NullLogger<OnboardingBehavior<CriarLancamentoCommand, CriarLancamentoCommandResult>>
                .Instance);

    private static Mock<IOnboardingService> Servico(ProgressoDoOnboarding progresso)
    {
        var servico = new Mock<IOnboardingService>();

        servico.Setup(s => s.ProgressoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(progresso);

        return servico;
    }

    private static CriarLancamentoCommand Lancamento() =>
        new(Tenant, Ana, "Mercado", 100m, Agosto, Agosto, null);

    private static async Task<CriarLancamentoCommandResult> Executar(
        OnboardingBehavior<CriarLancamentoCommand, CriarLancamentoCommandResult> behavior,
        Action? aoChamarHandler = null)
    {
        return await behavior.HandleAsync(
            Lancamento(),
            () =>
            {
                aoChamarHandler?.Invoke();
                return Task.FromResult(
                    CriarLancamentoCommandResult.Criado([Guid.CreateVersion7()]));
            },
            CancellationToken.None);
    }

    [Fact]
    public async Task Escrita_com_requisito_bloqueante_pendente_e_recusada_nomeando_o_requisito()
    {
        var chegouNoHandler = false;

        var resultado = await Executar(
            Behavior(Servico(ProgressoDoOnboarding.Montar(CatalogoDoProduto(), [])).Object),
            () => chegouNoHandler = true);

        Assert.False(resultado.Sucesso);
        Assert.False(chegouNoHandler);
        Assert.Equal(
            OnboardingBehavior<CriarLancamentoCommand, CriarLancamentoCommandResult>
                .CodigoOnboardingPendente,
            resultado.Erros[0].Codigo);
        Assert.Equal(
            OnboardingRequisitoEntity.CodigoRendaDeclarada, resultado.Erros[0].Campo);
    }

    [Fact]
    public async Task Escrita_liberada_apos_cumprir_os_requisitos_bloqueantes()
    {
        var chegouNoHandler = false;

        var resultado = await Executar(
            Behavior(Servico(ProgressoDoOnboarding.Montar(
                CatalogoDoProduto(),
                [Cumprido(OnboardingRequisitoEntity.CodigoRendaDeclarada)])).Object),
            () => chegouNoHandler = true);

        Assert.True(resultado.Sucesso);
        Assert.True(chegouNoHandler);
    }

    [Fact]
    public async Task Requisito_declarado_e_inativo_nao_bloqueia_a_escrita()
    {
        var resultado = await Executar(
            Behavior(Servico(ProgressoDoOnboarding.Montar(
                [Requisito("documentos", 1, ativo: false, bloqueante: true)], [])).Object));

        Assert.True(resultado.Sucesso);
    }

    /// <summary>
    /// Em tenant de terceiro quem recusa é a RLS. Recusar aqui daria à pessoa uma mensagem sobre
    /// o próprio onboarding quando o problema é que ela não pode escrever naquele espaço.
    /// </summary>
    [Fact]
    public async Task Tenant_de_terceiro_nao_passa_pela_avaliacao_de_onboarding()
    {
        var servico = Servico(ProgressoDoOnboarding.Montar(CatalogoDoProduto(), []));

        var resultado = await Executar(
            Behavior(servico.Object, new ContextoDeTeste { SomenteLeitura = true }));

        Assert.True(resultado.Sucesso);
        servico.Verify(s => s.ProgressoAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Sem_tenant_selecionado_o_behavior_nao_avalia()
    {
        var servico = Servico(ProgressoDoOnboarding.Montar(CatalogoDoProduto(), []));

        var resultado = await Executar(
            Behavior(servico.Object, new ContextoDeTeste { TenantId = null }));

        Assert.True(resultado.Sucesso);
        servico.Verify(s => s.ProgressoAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------------------------------------------------------------- pipeline

    /// <summary>
    /// Nenhuma Query passa pelo onboarding.
    /// </summary>
    /// <remarks>
    /// A garantia mais forte é do compilador: <c>OnboardingBehavior&lt;LancamentosQuery, ...&gt;</c>
    /// nem chega a ser um tipo válido, porque a restrição exige <c>IEscritaFinanceira</c>. O que
    /// este teste cobre é o passo seguinte — que o contêiner, ao compor o pipeline de uma
    /// consulta, também não o inclui. É o que sustenta a navegação liberada desde o primeiro
    /// acesso: as telas abrem e mostram estado vazio em vez de devolver erro.
    /// </remarks>
    [Fact]
    public void Consultas_nao_recebem_o_behavior_de_onboarding()
    {
        var provedor = Container();

        Assert.DoesNotContain(
            provedor.GetServices<IPipelineBehavior<LancamentosQuery, LancamentosQueryResult>>(),
            EhOnboarding);

        Assert.DoesNotContain(
            provedor.GetServices<
                IPipelineBehavior<ProgressoOnboardingQuery, ProgressoOnboardingQueryResult>>(),
            EhOnboarding);
    }

    [Fact]
    public void Commands_de_escrita_financeira_recebem_o_behavior_de_onboarding() =>
        Assert.Contains(
            Container().GetServices<
                IPipelineBehavior<CriarLancamentoCommand, CriarLancamentoCommandResult>>(),
            EhOnboarding);

    /// <summary>
    /// Cadastrar renda não pode ser bloqueado: é a ação que cumpre o requisito. Bloqueá-la
    /// trancaria a pessoa para fora do único caminho que destrava a conta.
    /// </summary>
    [Fact]
    public void Cadastro_de_renda_nao_recebe_o_behavior_de_onboarding() =>
        Assert.DoesNotContain(
            Container().GetServices<IPipelineBehavior<
                CadastrarFonteRendaCommand, CadastrarFonteRendaCommandResult>>(),
            EhOnboarding);

    private static void Dublar<T>(IServiceCollection services)
        where T : class =>
        services.AddScoped(_ => new Mock<T>().Object);

    private static bool EhOnboarding(object behavior)
    {
        var tipo = behavior.GetType();

        return tipo.IsGenericType
               && tipo.GetGenericTypeDefinition() == typeof(OnboardingBehavior<,>);
    }

    /// <summary>
    /// Monta o contêiner na mesma ordem do <c>Program.cs</c>: os repositórios ficam de fora
    /// porque o que está sob teste é quais behaviors o pipeline compõe, não o acesso ao banco.
    /// </summary>
    private static ServiceProvider Container()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddScoped<IContextoRequisicao>(_ => new ContextoDeTeste());

        // Os repositórios entram como dublês. Resolver os behaviors de verdade exige o grafo
        // inteiro, e é bom que exija: se um serviço novo ficar sem registro, este teste quebra
        // antes de a Api falhar no arranque.
        Dublar<IOnboardingWriteRepository>(services);
        Dublar<IAlertaWriteRepository>(services);
        Dublar<IRelatorioReadRepository>(services);
        Dublar<IOrcamentoReadRepository>(services);
        Dublar<ICategoriaReadRepository>(services);
        Dublar<IFonteRendaReadRepository>(services);
        Dublar<IRegraClassificacaoWriteRepository>(services);
        Dublar<IAssinaturaWriteRepository>(services);
        Dublar<IPlanoReadRepository>(services);

        services.AddServices();
        services.AddCore(typeof(ServicesServiceCollecation).Assembly);

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Progresso_sem_tenant_devolve_vazio()
    {
        var servico = new Mock<IOnboardingService>();

        var resultado = await new ProgressoOnboardingQueryHandler(servico.Object)
            .HandleAsync(new ProgressoOnboardingQuery(Guid.Empty), CancellationToken.None);

        Assert.Empty(resultado.Passos);
        servico.Verify(s => s.ProgressoAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Progresso_devolve_o_caminho_inteiro_e_o_que_falta()
    {
        var servico = Servico(ProgressoDoOnboarding.Montar(CatalogoDoProduto(), []));

        var resultado = await new ProgressoOnboardingQueryHandler(servico.Object)
            .HandleAsync(new ProgressoOnboardingQuery(Tenant), CancellationToken.None);

        Assert.Equal(3, resultado.Passos.Count);
        Assert.Single(resultado.Pendentes);
        Assert.False(resultado.Liberado);
        Assert.Equal(2, resultado.Passos.Count(passo => passo.Futuro));
    }

    [Fact]
    public Task Progresso_repassa_sem_cache()
    {
        var behavior = new ProgressoOnboardingQueryBehavior();

        return behavior.HandleAsync(
            new ProgressoOnboardingQuery(Tenant),
            () => Task.FromResult(ProgressoOnboardingQueryResult.Vazio),
            CancellationToken.None);
    }

    /// <summary>
    /// Todo Command de escrita financeira sabe se recusar pelo contrato genérico.
    /// </summary>
    /// <remarks>
    /// Cada resultado tem sua fábrica com o nome que cabe ao contexto — <c>Recusado</c>,
    /// <c>Recusada</c> — e a implementação explícita liga as duas. Ligar errado passaria pelo
    /// compilador e só apareceria quando o onboarding bloqueasse aquele Command específico, num
    /// resultado de sucesso vazio que a tela leria como "gravou".
    /// </remarks>
    [Fact]
    public void Todo_resultado_de_escrita_financeira_sabe_se_recusar()
    {
        Recusa<CriarLancamentoCommandResult>(resultado => resultado.Sucesso);
        Recusa<Services.V1.Lancamentos.Command.AlterarLancamento.AlterarLancamentoCommandResult>(
            resultado => resultado.Sucesso);
        Recusa<Services.V1.Lancamentos.Command.RemoverLancamento.RemoverLancamentoCommandResult>(
            resultado => resultado.Sucesso);
        Recusa<Services.V1.Lancamentos.Command.CriarCategoria.CriarCategoriaCommandResult>(
            resultado => resultado.Sucesso);
        Recusa<Services.V1.Lancamentos.Command.CriarRecorrencia.CriarRecorrenciaCommandResult>(
            resultado => resultado.Sucesso);
        Recusa<Services.V1.Lancamentos.Command.EncerrarRecorrencia.EncerrarRecorrenciaCommandResult>(
            resultado => resultado.Sucesso);
        Recusa<Services.V1.Classificacao.Command.ClassificarLancamento.ClassificarLancamentoCommandResult>(
            resultado => resultado.Sucesso);
        Recusa<Services.V1.Planejamento.Command.DefinirOrcamento.DefinirOrcamentoCommandResult>(
            resultado => resultado.Sucesso);
        Recusa<Services.V1.Planejamento.Command.CopiarOrcamento.CopiarOrcamentoCommandResult>(
            resultado => resultado.Sucesso);
    }

    private static void Recusa<TResult>(Func<TResult, bool> sucesso)
        where TResult : IResultadoRecusavel<TResult>
    {
        var resultado = TResult.Recusar(new Error("motivo", "Não deu."));

        Assert.False(sucesso(resultado));
    }

    [Fact]
    public void Assembly_do_behavior_e_o_da_camada_de_aplicacao() =>
        Assert.Equal(
            typeof(ServicesServiceCollecation).Assembly,
            typeof(OnboardingBehavior<,>).GetTypeInfo().Assembly);
}
