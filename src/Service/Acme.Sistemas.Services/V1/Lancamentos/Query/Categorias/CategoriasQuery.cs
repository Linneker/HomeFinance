using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Lancamentos.Query.Categorias;

public sealed record CategoriasQuery(Guid TenantId) : IRequest<CategoriasQueryResult>;
