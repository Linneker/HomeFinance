using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Planejamento.Query.FontesRenda;

/// <param name="Competencia">
/// Mês de interesse. Nulo devolve o histórico inteiro de vigências — é a visão de auditoria da
/// renda, não a da tela do mês.
/// </param>
public sealed record FontesRendaQuery(Guid TenantId, DateOnly? Competencia)
    : IRequest<FontesRendaQueryResult>;
