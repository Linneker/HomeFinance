using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Services.V1.Relatorios.Query.ExtratoDoMes;

/// <param name="Valor">Valor apurado do tenant, e não o valor cheio do lançamento.</param>
/// <param name="OrcamentoId">
/// Previsão orçamentária a que o gasto pertence. Nulo quando a categoria não tem valor
/// planejado no mês — ou quando o lançamento ainda nem tem categoria.
/// </param>
/// <param name="ForaDoPlanejamento">
/// Categoria conhecida, mas sem orçamento no mês. É diferente de
/// <paramref name="PendenteDeClassificacao"/>: ali não se sabe ainda a que plano o gasto
/// pertenceria, aqui já se sabe que não pertence a nenhum.
/// </param>
public sealed record ItemDoExtrato(
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

/// <param name="Vazio">
/// O mês não tem lançamento nenhum. Explícito para a tela distinguir "não há registros" de
/// "ainda carregando", que é a diferença entre informar e parecer travado.
/// </param>
public sealed record ExtratoDoMesQueryResult(
    DateOnly Competencia,
    bool VisaoPorCaixa,
    IReadOnlyList<ItemDoExtrato> Lancamentos,
    decimal Total,
    int QuantidadePendente,
    bool Vazio);
