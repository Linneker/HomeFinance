using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Services.V1.Alertas.Query.AlertasAbertos;
using Acme.Sistemas.Services.V1.Assinaturas;
using Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;
using Acme.Sistemas.Services.V1.Identidade.Command.ExcluirConta;
using Acme.Sistemas.Services.V1.Lancamentos.Command.CriarLancamento;
using Acme.Sistemas.Services.V1.Relatorios.Query.ExtratoDoMes;
using Acme.Sistemas.Services.V1.Relatorios.Query.ResumoDoMes;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

/// <summary>
/// O que a assinatura suspensa restringe, o que ela preserva, e por que essa fronteira é
/// independente da do onboarding.
/// </summary>
/// <remarks>
/// A divisão é o coração da política de inadimplência: registrar e ler os próprios lançamentos
/// continua funcionando com a assinatura suspensa — tirar o acesso ao que a pessoa digitou seria
/// reter o dado dela como refém. O que para é o que o produto <em>produz</em> em cima disso.
/// </remarks>
public sealed class AssinaturaBehaviorTests
{
    private static readonly Guid Tenant = Guid.CreateVersion7();
    private static readonly DateOnly Agosto = new(2026, 8, 1);

    private static IAssinaturaService Situacao(SituacaoAssinatura situacao)
    {
        var assinaturas = new Mock<IAssinaturaService>();

        assinaturas.Setup(a => a.SituacaoAtualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SituacaoDaConta(situacao, "casal", 3, 0));

        return assinaturas.Object;
    }

    private static AssinaturaBehavior<TRequest, TResult> Behavior<TRequest, TResult>(
        SituacaoAssinatura situacao)
        where TRequest : IRequest<TResult>, IRecursoDerivado
        where TResult : IResultadoRecusavel<TResult> =>
        new(
            Situacao(situacao),
            NullLogger<AssinaturaBehavior<TRequest, TResult>>.Instance);

    private static async Task<(TResult Resultado, bool Chamou)> Executar<TRequest, TResult>(
        SituacaoAssinatura situacao,
        TRequest request,
        TResult doHandler)
        where TRequest : IRequest<TResult>, IRecursoDerivado
        where TResult : IResultadoRecusavel<TResult>
    {
        var chamou = false;

        var resultado = await Behavior<TRequest, TResult>(situacao).HandleAsync(
            request,
            () =>
            {
                chamou = true;
                return Task.FromResult(doHandler);
            },
            CancellationToken.None);

        return (resultado, chamou);
    }

    // ---------------------------------------------------------------- recursos derivados

    /// <summary>Cenário da spec: condição que geraria alerta num tenant suspenso não é emitida.</summary>
    [Fact]
    public async Task Alertas_ficam_indisponiveis_com_assinatura_suspensa()
    {
        var (resultado, chamou) = await Executar(
            SituacaoAssinatura.Suspensa,
            new AlertasAbertosQuery(Tenant, SomenteLeitura: false),
            AlertasAbertosQueryResult.Vazio);

        Assert.False(chamou);
        Assert.Equal(
            AssinaturaBehavior<AlertasAbertosQuery, AlertasAbertosQueryResult>.CodigoAssinaturaInativa,
            resultado.Erros[0].Codigo);
    }

    /// <summary>Cenário da spec: o resumo por categoria do mês não é apresentado.</summary>
    [Fact]
    public async Task Resumo_por_categoria_fica_indisponivel_com_assinatura_suspensa()
    {
        var (resultado, chamou) = await Executar(
            SituacaoAssinatura.Suspensa,
            new ResumoDoMesQuery(Tenant, Agosto),
            ResumoDoMesQueryResult.Vazio(Agosto, false));

        Assert.False(chamou);
        Assert.Equal(
            AssinaturaBehavior<ResumoDoMesQuery, ResumoDoMesQueryResult>.CodigoAssinaturaInativa,
            resultado.Erros[0].Codigo);
    }

    /// <summary>
    /// Ativa e carência são "vigente": os recursos derivados funcionam nas duas. Cortar quem está
    /// em carência tiraria o produto de quem o sistema decidiu tolerar.
    /// </summary>
    [Theory]
    [InlineData(SituacaoAssinatura.Ativa)]
    [InlineData(SituacaoAssinatura.Carencia)]
    public async Task Recurso_derivado_passa_com_assinatura_vigente(SituacaoAssinatura situacao)
    {
        var esperado = ResumoDoMesQueryResult.Vazio(Agosto, true);

        var (resultado, chamou) = await Executar(
            situacao, new ResumoDoMesQuery(Tenant, Agosto), esperado);

        Assert.True(chamou);
        Assert.Same(esperado, resultado);
        Assert.Empty(resultado.Erros);
    }

    /// <summary>
    /// Concessão de compartilhamento é recurso derivado: com assinatura suspensa o Behavior a
    /// recusa antes de chegar ao handler, e a regra de cota nem é consultada.
    /// </summary>
    [Fact]
    public async Task Concessao_de_compartilhamento_e_recusada_com_assinatura_suspensa()
    {
        var (resultado, chamou) = await Executar(
            SituacaoAssinatura.Suspensa,
            new CompartilharTenantCommand(
                Guid.CreateVersion7(), Tenant, "bruno@teste.local",
                ConsentimentoConfirmado: true, IncluiScr: false),
            CompartilharTenantCommandResult.Concedido(Guid.CreateVersion7()));

        Assert.False(chamou);
        Assert.False(resultado.Sucesso);
        Assert.Equal(
            AssinaturaBehavior<CompartilharTenantCommand, CompartilharTenantCommandResult>
                .CodigoAssinaturaInativa,
            resultado.Erros[0].Codigo);
    }

    // ---------------------------------------------------------------- fronteira dos marcadores

    /// <summary>
    /// O que a suspensão preserva não passa pelo Behavior de assinatura, e é isso que o mantém
    /// preservado: registrar lançamento, ler o próprio extrato e excluir a conta não são recursos
    /// derivados. Se um deles ganhasse a marca, a suspensão passaria a barrá-lo — e este teste
    /// quebra antes disso.
    /// </summary>
    [Theory]
    [InlineData(typeof(CriarLancamentoCommand))]
    [InlineData(typeof(ExtratoDoMesQuery))]
    [InlineData(typeof(ExcluirContaCommand))]
    public void Registro_leitura_e_exclusao_nao_sao_recursos_derivados(Type request) =>
        Assert.False(typeof(IRecursoDerivado).IsAssignableFrom(request));

    /// <summary>
    /// Onboarding restringe a entrada (escrita de dado financeiro); assinatura restringe a saída
    /// derivada. São conjuntos distintos: por isso a spec exige que sejam avaliados de forma
    /// independente, e por isso são dois Behaviors, não um parametrizado.
    /// </summary>
    [Fact]
    public void Restricao_de_onboarding_e_de_assinatura_atuam_sobre_conjuntos_distintos()
    {
        // Criar lançamento é entrada e não é derivado: quem o barra é o onboarding, nunca a
        // assinatura.
        Assert.True(typeof(IEscritaFinanceira).IsAssignableFrom(typeof(CriarLancamentoCommand)));
        Assert.False(typeof(IRecursoDerivado).IsAssignableFrom(typeof(CriarLancamentoCommand)));

        // Resumo por categoria é saída derivada e não é entrada: quem o barra é a assinatura,
        // nunca o onboarding.
        Assert.True(typeof(IRecursoDerivado).IsAssignableFrom(typeof(ResumoDoMesQuery)));
        Assert.False(typeof(IEscritaFinanceira).IsAssignableFrom(typeof(ResumoDoMesQuery)));

        // Concessão de compartilhamento é derivada, mas não é escrita de dado financeiro: só a
        // assinatura a restringe.
        Assert.True(typeof(IRecursoDerivado).IsAssignableFrom(typeof(CompartilharTenantCommand)));
        Assert.False(typeof(IEscritaFinanceira).IsAssignableFrom(typeof(CompartilharTenantCommand)));
    }
}
