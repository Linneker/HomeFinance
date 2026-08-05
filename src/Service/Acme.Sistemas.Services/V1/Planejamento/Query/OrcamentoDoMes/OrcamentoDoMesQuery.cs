using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Planejamento.Query.OrcamentoDoMes;

public sealed record OrcamentoDoMesQuery(Guid TenantId, DateOnly Competencia)
    : IRequest<OrcamentoDoMesQueryResult>;
