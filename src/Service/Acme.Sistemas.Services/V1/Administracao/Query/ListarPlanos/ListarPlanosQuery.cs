using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Administracao.Query.ListarPlanos;

/// <summary>Catálogo completo para o back-office — inclui os planos inativos.</summary>
public sealed record ListarPlanosQuery : IRequest<ListarPlanosQueryResult>;
