using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Classificacao.Query.Regras;

public sealed record RegrasQuery(Guid TenantId) : IRequest<RegrasQueryResult>;
