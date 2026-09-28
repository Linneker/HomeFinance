using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Exportacao.Command.HabilitarExportacao;

/// <summary>Habilita a exportação para uma conta — ação exclusiva da administração.</summary>
/// <param name="AutorAdminId">Administrador autenticado. A autorização é imposta no endpoint.</param>
public sealed record HabilitarExportacaoCommand(Guid ContaUsuarioId, string Motivo, Guid AutorAdminId)
    : IRequest<HabilitarExportacaoCommandResult>;
