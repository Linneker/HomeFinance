using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Relatorios.Query.ResumoDoMes;

/// <remarks>
/// Recurso derivado: é análise sobre os dados, não os dados. Com assinatura suspensa o extrato
/// continua abrindo — ele é o registro da pessoa — e o resumo não, porque é o produto que o
/// sistema constrói em cima dele.
/// </remarks>
public sealed record ResumoDoMesQuery(Guid TenantId, DateOnly Competencia, bool PorCaixa = false)
    : IRequest<ResumoDoMesQueryResult>, IRecursoDerivado;
