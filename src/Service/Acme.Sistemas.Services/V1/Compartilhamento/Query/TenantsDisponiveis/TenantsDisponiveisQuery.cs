using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Query.TenantsDisponiveis;

public sealed record TenantsDisponiveisQuery(Guid UsuarioId) : IRequest<TenantsDisponiveisQueryResult>;
