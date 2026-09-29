using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Query.ListarCompartilhamentos;

/// <summary>Compartilhamentos vigentes concedidos a partir do tenant do titular.</summary>
public sealed record ListarCompartilhamentosQuery(Guid TenantId) : IRequest<ListarCompartilhamentosQueryResult>;
