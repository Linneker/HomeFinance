using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Exportacao.Query.SituacaoExportacao;

/// <summary>Situação de uma solicitação de exportação do tenant ativo.</summary>
/// <remarks>
/// Não é recurso derivado: a exportação prevalece sobre a restrição de assinatura, e o titular
/// precisa acompanhar a geração mesmo com a assinatura suspensa.
/// </remarks>
public sealed record SituacaoExportacaoQuery(Guid TenantId, Guid SolicitacaoId)
    : IRequest<SituacaoExportacaoQueryResult>;
