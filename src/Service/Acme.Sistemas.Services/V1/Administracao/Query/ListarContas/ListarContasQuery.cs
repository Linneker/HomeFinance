using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Administracao.Query.ListarContas;

/// <summary>Relação de contas para o back-office — identificação e situação, sem dado financeiro.</summary>
public sealed record ListarContasQuery : IRequest<ListarContasQueryResult>;
