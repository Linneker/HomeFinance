using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Services.V1.Alertas.Query.AlertasAbertos;
using Acme.Sistemas.Services.V1.Classificacao.Query.Regras;
using Acme.Sistemas.Services.V1.Compartilhamento.Query.ItensDoConsentimento;
using Acme.Sistemas.Services.V1.Compartilhamento.Query.TenantsDisponiveis;
using Acme.Sistemas.Services.V1.Lancamentos.Query.Categorias;
using Acme.Sistemas.Services.V1.Lancamentos.Query.Lancamentos;
using Acme.Sistemas.Services.V1.Planejamento.Query.FontesRenda;
using Acme.Sistemas.Services.V1.Planejamento.Query.OrcamentoDoMes;
using Acme.Sistemas.Services.V1.Relatorios.Query.ExtratoDoMes;
using Acme.Sistemas.Services.V1.Relatorios.Query.ResumoDoMes;

namespace Acme.Sistemas.Services.UnitTest.Test;

/// <summary>
/// Behaviors das Queries.
/// </summary>
/// <remarks>
/// Todos repassam sem tocar no resultado — nenhuma Query do produto pode cachear, porque o dado
/// é por tenant e um cache mal escopado mostraria o extrato de um cliente na tela de outro. O
/// teste existe para que introduzir cache em qualquer um deles quebre aqui, e não em produção.
/// </remarks>
public sealed class BehaviorsTests
{
    private static readonly Guid Tenant = Guid.CreateVersion7();
    private static readonly Guid Usuario = Guid.CreateVersion7();
    private static readonly DateOnly Agosto = new(2026, 8, 1);

    private static async Task RepassaSemAlterar<TRequest, TResult>(
        IPipelineBehavior<TRequest, TResult> behavior,
        TRequest request,
        TResult esperado)
        where TRequest : IRequest<TResult>
        where TResult : class
    {
        var chamou = false;

        var resultado = await behavior.HandleAsync(
            request,
            () =>
            {
                chamou = true;
                return Task.FromResult(esperado);
            },
            CancellationToken.None);

        Assert.True(chamou);
        Assert.Same(esperado, resultado);
    }

    [Fact]
    public Task Regras_repassa_sem_cache() =>
        RepassaSemAlterar(
            new RegrasQueryBehavior(), new RegrasQuery(Tenant), new RegrasQueryResult([]));

    [Fact]
    public Task Categorias_repassa_sem_cache() =>
        RepassaSemAlterar(
            new CategoriasQueryBehavior(),
            new CategoriasQuery(Tenant),
            new CategoriasQueryResult([]));

    [Fact]
    public Task Lancamentos_repassa_sem_cache() =>
        RepassaSemAlterar(
            new LancamentosQueryBehavior(),
            new LancamentosQuery(Tenant, Agosto, Agosto),
            new LancamentosQueryResult([], false));

    [Fact]
    public Task Tenants_disponiveis_repassa_sem_cache() =>
        RepassaSemAlterar(
            new TenantsDisponiveisQueryBehavior(),
            new TenantsDisponiveisQuery(Usuario),
            new TenantsDisponiveisQueryResult([]));

    [Fact]
    public Task Itens_do_consentimento_repassa_sem_cache() =>
        RepassaSemAlterar(
            new ItensDoConsentimentoQueryBehavior(),
            new ItensDoConsentimentoQuery(Usuario),
            new ItensDoConsentimentoQueryResult([]));

    [Fact]
    public Task Fontes_de_renda_repassa_sem_cache() =>
        RepassaSemAlterar(
            new FontesRendaQueryBehavior(),
            new FontesRendaQuery(Tenant, Agosto),
            new FontesRendaQueryResult([], 0m));

    [Fact]
    public Task Orcamento_do_mes_repassa_sem_cache() =>
        RepassaSemAlterar(
            new OrcamentoDoMesQueryBehavior(),
            new OrcamentoDoMesQuery(Tenant, Agosto),
            new OrcamentoDoMesQueryResult(Agosto, [], 0m, 0m, null));

    [Fact]
    public Task Extrato_do_mes_repassa_sem_cache() =>
        RepassaSemAlterar(
            new ExtratoDoMesQueryBehavior(),
            new ExtratoDoMesQuery(Tenant, Agosto),
            new ExtratoDoMesQueryResult(Agosto, false, [], 0m, 0, true));

    [Fact]
    public Task Resumo_do_mes_repassa_sem_cache() =>
        RepassaSemAlterar(
            new ResumoDoMesQueryBehavior(),
            new ResumoDoMesQuery(Tenant, Agosto),
            ResumoDoMesQueryResult.Vazio(Agosto, false));

    [Fact]
    public Task Alertas_abertos_repassa_sem_cache() =>
        RepassaSemAlterar(
            new AlertasAbertosQueryBehavior(),
            new AlertasAbertosQuery(Tenant, SomenteLeitura: false),
            AlertasAbertosQueryResult.Vazio);
}
