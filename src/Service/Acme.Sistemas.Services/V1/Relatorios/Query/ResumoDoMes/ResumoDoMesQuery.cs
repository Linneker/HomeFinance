using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Relatorios.Query.ResumoDoMes;

public sealed record ResumoDoMesQuery(Guid TenantId, DateOnly Competencia, bool PorCaixa = false)
    : IRequest<ResumoDoMesQueryResult>;
