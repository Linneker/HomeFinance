using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Alertas.Command.DispensarAlerta;

/// <summary>
/// Marca o alerta como dispensado.
/// </summary>
/// <remarks>
/// Dispensar não encerra: a condição continua valendo e o alerta continua bloqueando a emissão
/// de um novo aviso igual. É o que faz "não me avise disso" durar até o problema realmente
/// acabar — encerrar aqui faria o mesmo aviso voltar no próximo lançamento registrado.
/// </remarks>
public sealed class DispensarAlertaCommandHandler(
    IAlertaWriteRepository alertas,
    TimeProvider relogio)
    : IRequestHandler<DispensarAlertaCommand, DispensarAlertaCommandResult>
{
    public const string CodigoAlertaNaoEncontrado = "alerta_nao_encontrado";
    public const string CodigoAlertaNaoDispensavel = "alerta_nao_dispensavel";

    public async Task<DispensarAlertaCommandResult> HandleAsync(
        DispensarAlertaCommand request,
        CancellationToken cancellationToken)
    {
        var erros = DispensarAlertaCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return DispensarAlertaCommandResult.Recusado(erros[0]);
        }

        var alerta = await alertas.ObterAsync(request.AlertaId, cancellationToken);

        if (alerta is null)
        {
            return DispensarAlertaCommandResult.Recusado(
                new Error(CodigoAlertaNaoEncontrado, "Alerta não encontrado."));
        }

        // Já encerrado ou já dispensado não se dispensa de novo. Aceitar em silêncio moveria a
        // data de dispensa de um alerta antigo e apagaria quando a pessoa de fato decidiu.
        if (alerta.Estado != EstadoAlerta.Aberto)
        {
            return DispensarAlertaCommandResult.Recusado(
                new Error(CodigoAlertaNaoDispensavel, "Este alerta não está mais aberto."));
        }

        alerta.Dispensar(relogio.GetUtcNow());

        return await alertas.AtualizarEstadoAsync(alerta, cancellationToken)
            ? DispensarAlertaCommandResult.Dispensado()
            : DispensarAlertaCommandResult.Recusado(
                new Error(CodigoAlertaNaoEncontrado, "Alerta não encontrado."));
    }
}
