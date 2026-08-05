using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.CriarCategoria;

/// <param name="TenantId">Tenant ativo, resolvido pelo middleware. Nunca vem do corpo.</param>
public sealed record CriarCategoriaCommand(
    Guid TenantId,
    string Nome) : IRequest<CriarCategoriaCommandResult>, IEscritaFinanceira;
