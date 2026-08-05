using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Alertas.Command.DispensarAlerta;

public static class DispensarAlertaCommandValidation
{
    public const string CodigoAlertaObrigatorio = "alerta_obrigatorio";

    public static IReadOnlyList<Error> Validar(DispensarAlertaCommand command) =>
        command.AlertaId == Guid.Empty
            ? [new Error(CodigoAlertaObrigatorio, "Alerta não identificado.", nameof(command.AlertaId))]
            : [];
}
