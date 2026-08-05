using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Alertas.Command.DispensarAlerta;

public sealed record DispensarAlertaCommandResult
{
    public bool Sucesso { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static DispensarAlertaCommandResult Dispensado() => new() { Sucesso = true };

    public static DispensarAlertaCommandResult Recusado(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };
}
