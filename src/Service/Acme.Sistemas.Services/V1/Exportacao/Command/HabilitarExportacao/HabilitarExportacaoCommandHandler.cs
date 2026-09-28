using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Entities.Auditoria;
using Acme.Sistemas.Domain.Entities.Exportacao;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Exportacao.Command.HabilitarExportacao;

/// <summary>
/// Grava a habilitação, registra na auditoria e avisa o titular.
/// </summary>
/// <remarks>
/// O aviso é chamada direta, fora do pipeline de assinatura: ele tem que chegar mesmo com a
/// assinatura do titular suspensa. Habilitar não concede leitura ao administrador — a gravação
/// não seleciona tenant nenhum, e o dado financeiro segue invisível para ele.
/// </remarks>
public sealed class HabilitarExportacaoCommandHandler(
    IExportacaoHabilitacaoWriteRepository habilitacoes,
    IAuditoriaRepository auditoria,
    IAvisoTitular avisos,
    TimeProvider relogio)
    : IRequestHandler<HabilitarExportacaoCommand, HabilitarExportacaoCommandResult>
{
    public async Task<HabilitarExportacaoCommandResult> HandleAsync(
        HabilitarExportacaoCommand request,
        CancellationToken cancellationToken)
    {
        var erros = HabilitarExportacaoCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return HabilitarExportacaoCommandResult.Recusada(erros[0]);
        }

        var agora = relogio.GetUtcNow();

        await habilitacoes.DefinirAsync(
            new ExportacaoHabilitacaoEntity
            {
                Id = Guid.CreateVersion7(),
                UsuarioId = request.ContaUsuarioId,
                Habilitada = true,
                Motivo = request.Motivo,
                AutorId = request.AutorAdminId,
                CriadoEm = agora,
            },
            cancellationToken);

        await auditoria.RegistrarAsync(
            new RegistroAuditoriaEntity
            {
                Id = Guid.CreateVersion7(),
                Evento = EventoAuditoria.ExportacaoHabilitada,
                ContaUsuarioId = request.ContaUsuarioId,
                AutorId = request.AutorAdminId,
                Detalhe = request.Motivo,
                CriadoEm = agora,
            },
            cancellationToken);

        await avisos.AvisarAsync(
            request.ContaUsuarioId,
            "Exportação de dados disponível",
            "O recurso de exportação foi habilitado para a sua conta.",
            cancellationToken);

        return HabilitarExportacaoCommandResult.Habilitada();
    }
}
