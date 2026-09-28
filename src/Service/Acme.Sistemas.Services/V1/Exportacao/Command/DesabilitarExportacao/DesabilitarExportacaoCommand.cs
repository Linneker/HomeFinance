using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Exportacao.Command.DesabilitarExportacao;

/// <summary>Desabilita a exportação de uma conta — ação exclusiva da administração.</summary>
public sealed record DesabilitarExportacaoCommand(Guid ContaUsuarioId, string Motivo, Guid AutorAdminId)
    : IRequest<DesabilitarExportacaoCommandResult>;
