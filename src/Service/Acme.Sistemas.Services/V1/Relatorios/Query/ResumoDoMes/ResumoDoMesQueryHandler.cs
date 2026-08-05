using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Entities.Relatorios;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Relatorios.Query.ResumoDoMes;

/// <summary>
/// Planejado, realizado e diferença por categoria, mais o que ficou fora do plano.
/// </summary>
/// <remarks>
/// A lista de categorias é montada a partir do orçamento, não dos gastos: é o plano que define
/// o que a pessoa quer acompanhar. Gasto em categoria não orçada não vira linha — vira parte do
/// bloco "fora do planejamento", junto com o que ainda está pendente de classificação.
/// </remarks>
public sealed class ResumoDoMesQueryHandler(
    IRelatorioReadRepository relatorios,
    IOrcamentoReadRepository orcamentos)
    : IRequestHandler<ResumoDoMesQuery, ResumoDoMesQueryResult>
{
    public async Task<ResumoDoMesQueryResult> HandleAsync(
        ResumoDoMesQuery request,
        CancellationToken cancellationToken)
    {
        var competencia = CalendarioFinanceiro.PrimeiroDiaDoMes(request.Competencia);

        if (ResumoDoMesQueryValidation.Validar(request).Count > 0)
        {
            return ResumoDoMesQueryResult.Vazio(competencia, request.PorCaixa);
        }

        var periodo = new PeriodoDoRelatorio(
            competencia,
            CalendarioFinanceiro.UltimoDiaDoMes(competencia),
            request.PorCaixa);

        var totais = await relatorios.TotaisPorCategoriaAsync(periodo, cancellationToken);
        var planejado = await orcamentos.ListarDoMesAsync(competencia, cancellationToken);

        var realizadoPorCategoria = totais
            .Where(total => total.CategoriaId is not null)
            .ToDictionary(total => total.CategoriaId!.Value);

        var categorias = planejado
            .Select(item =>
            {
                var realizado = realizadoPorCategoria.GetValueOrDefault(item.CategoriaId);

                return new LinhaDoResumo(
                    item.CategoriaId,
                    item.ValorPlanejado,
                    realizado?.Realizado ?? 0m,
                    realizado?.Quantidade ?? 0);
            })
            .ToList();

        var orcadas = planejado.Select(item => item.CategoriaId).ToHashSet();

        var semPlano = totais
            .Where(total => total.CategoriaId is not null && !orcadas.Contains(total.CategoriaId.Value))
            .ToList();

        var pendente = totais
            .Where(total => total.CategoriaId is null)
            .Sum(total => total.Realizado);

        var fora = new ForaDoPlanejamento(
            semPlano,
            semPlano.Sum(total => total.Realizado) + pendente,
            pendente);

        return new ResumoDoMesQueryResult(
            competencia,
            request.PorCaixa,
            categorias,
            fora,
            planejado.Sum(item => item.ValorPlanejado),
            totais.Sum(total => total.Realizado));
    }
}
