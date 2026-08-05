using Acme.Sistemas.Services.V1.Relatorios.Query.ExtratoDoMes;
using Acme.Sistemas.Services.V1.Relatorios.Query.ResumoDoMes;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Relatorio;

internal static class RelatorioMap
{
    public static ExtratoResponse ParaResponse(this ExtratoDoMesQueryResult resultado) =>
        new(
            resultado.Competencia,
            resultado.VisaoPorCaixa,
            [.. resultado.Lancamentos.Select(item => new ItemDoExtratoResponse(
                item.Id,
                item.Descricao,
                item.Valor,
                item.DataCompetencia,
                item.DataCaixa,
                item.CategoriaId,
                item.PendenteDeClassificacao,
                item.ForaDoPlanejamento,
                item.OrcamentoId,
                item.ValorPlanejadoDaCategoria,
                item.Tipo,
                item.Parcelamento,
                item.Previsto))],
            resultado.Total,
            resultado.QuantidadePendente,
            resultado.Vazio);

    public static ResumoResponse ParaResponse(this ResumoDoMesQueryResult resultado) =>
        new(
            resultado.Competencia,
            resultado.VisaoPorCaixa,
            [.. resultado.Categorias.Select(linha => new LinhaDoResumoResponse(
                linha.CategoriaId,
                linha.Planejado,
                linha.Realizado,
                linha.Quantidade,
                linha.Excedida,
                linha.Disponivel,
                linha.Excedente))],
            new ForaDoPlanejamentoResponse(
                [.. resultado.ForaDoPlanejamento.Categorias.Select(total =>
                    new TotalSemPlanoResponse(
                        total.CategoriaId, total.Realizado, total.Quantidade))],
                resultado.ForaDoPlanejamento.Total,
                resultado.ForaDoPlanejamento.PendenteDeClassificacao),
            resultado.TotalPlanejado,
            resultado.TotalRealizado,
            resultado.Diferenca);
}
