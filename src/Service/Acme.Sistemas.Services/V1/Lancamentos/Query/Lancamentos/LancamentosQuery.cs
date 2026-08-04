using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Lancamentos.Query.Lancamentos;

/// <param name="PorCaixa">
/// Falso é competência, a visão primária. A escolha vira a coluna filtrada e ordenada — as
/// duas visões são a mesma consulta sobre colunas diferentes.
/// </param>
public sealed record LancamentosQuery(
    Guid TenantId,
    DateOnly? De,
    DateOnly? Ate,
    bool PorCaixa = false,
    Guid? CategoriaId = null) : IRequest<LancamentosQueryResult>;
