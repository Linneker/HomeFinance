using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Entities.Auditoria;
using Acme.Sistemas.Domain.Entities.Exportacao;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Exportacao.Command.DesabilitarExportacao;

/// <summary>
/// Desliga a habilitação e cancela as exportações da conta que ainda não concluíram.
/// </summary>
/// <remarks>
/// "Exportações não concluídas são canceladas": desabilitar não pode deixar uma geração já
/// enfileirada terminar e entregar o arquivo depois de a operação ter fechado o recurso. O
/// cancelamento roda por função definer, porque parte do administrador, sem o tenant do titular.
/// </remarks>
public sealed class DesabilitarExportacaoCommandHandler(
    IExportacaoHabilitacaoWriteRepository habilitacoes,
    IExportacaoSolicitacaoWriteRepository solicitacoes,
    IAuditoriaRepository auditoria,
    TimeProvider relogio)
    : IRequestHandler<DesabilitarExportacaoCommand, DesabilitarExportacaoCommandResult>
{
    public async Task<DesabilitarExportacaoCommandResult> HandleAsync(
        DesabilitarExportacaoCommand request,
        CancellationToken cancellationToken)
    {
        var erros = DesabilitarExportacaoCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return DesabilitarExportacaoCommandResult.Recusada(erros[0]);
        }

        var agora = relogio.GetUtcNow();

        await habilitacoes.DefinirAsync(
            new ExportacaoHabilitacaoEntity
            {
                Id = Guid.CreateVersion7(),
                UsuarioId = request.ContaUsuarioId,
                Habilitada = false,
                Motivo = request.Motivo,
                AutorId = request.AutorAdminId,
                CriadoEm = agora,
            },
            cancellationToken);

        var canceladas = await solicitacoes.CancelarPendentesAsync(request.ContaUsuarioId, cancellationToken);

        await auditoria.RegistrarAsync(
            new RegistroAuditoriaEntity
            {
                Id = Guid.CreateVersion7(),
                Evento = EventoAuditoria.ExportacaoDesabilitada,
                ContaUsuarioId = request.ContaUsuarioId,
                AutorId = request.AutorAdminId,
                Detalhe = request.Motivo,
                CriadoEm = agora,
            },
            cancellationToken);

        return DesabilitarExportacaoCommandResult.Desabilitada(canceladas);
    }
}
