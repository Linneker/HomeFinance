using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Administracao.Command.AutenticarAdministrador;

public sealed record AutenticarAdministradorCommandResult
{
    public bool Sucesso { get; init; }

    public string? Token { get; init; }

    public DateTimeOffset? ExpiraEm { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static AutenticarAdministradorCommandResult Autenticado(string token, DateTimeOffset expiraEm) => new()
    {
        Sucesso = true,
        Token = token,
        ExpiraEm = expiraEm,
    };

    public static AutenticarAdministradorCommandResult Recusado(Error erro) => new()
    {
        Sucesso = false,
        Erros = [erro],
    };
}
