using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Relatorio;

public sealed record ItemDoExtratoResponse(
    Guid Id,
    string Descricao,
    decimal Valor,
    DateOnly DataCompetencia,
    DateOnly DataCaixa,
    Guid? CategoriaId,
    bool PendenteDeClassificacao,
    bool ForaDoPlanejamento,
    Guid? OrcamentoId,
    decimal? ValorPlanejadoDaCategoria,
    TipoLancamento Tipo,
    string? Parcelamento,
    bool Previsto);

/// <param name="VisaoPorCaixa">
/// Qual das duas leituras este corpo representa. Sempre explícito: dois totais diferentes para
/// o mesmo mês pareceriam erro do sistema sem este rótulo.
/// </param>
public sealed record ExtratoResponse(
    DateOnly Competencia,
    bool VisaoPorCaixa,
    IReadOnlyList<ItemDoExtratoResponse> Lancamentos,
    decimal Total,
    int QuantidadePendente,
    bool Vazio);

public sealed record LinhaDoResumoResponse(
    Guid CategoriaId,
    decimal Planejado,
    decimal Realizado,
    int Quantidade,
    bool Excedida,
    decimal Disponivel,
    decimal Excedente);

public sealed record TotalSemPlanoResponse(Guid? CategoriaId, decimal Realizado, int Quantidade);

public sealed record ForaDoPlanejamentoResponse(
    IReadOnlyList<TotalSemPlanoResponse> Categorias,
    decimal Total,
    decimal PendenteDeClassificacao);

public sealed record ResumoResponse(
    DateOnly Competencia,
    bool VisaoPorCaixa,
    IReadOnlyList<LinhaDoResumoResponse> Categorias,
    ForaDoPlanejamentoResponse ForaDoPlanejamento,
    decimal TotalPlanejado,
    decimal TotalRealizado,
    decimal Diferenca);
