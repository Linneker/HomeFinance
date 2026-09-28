using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Exportacao.Query.BaixarExportacao;

/// <summary>Baixa o arquivo de uma exportação concluída.</summary>
/// <param name="UsuarioId">
/// Conta que está baixando. O download é vinculado a quem solicitou: mesmo dentro do tenant, um
/// destinatário de compartilhamento não baixa o arquivo que o dono gerou.
/// </param>
public sealed record BaixarExportacaoQuery(Guid TenantId, Guid UsuarioId, Guid SolicitacaoId)
    : IRequest<BaixarExportacaoQueryResult>;
