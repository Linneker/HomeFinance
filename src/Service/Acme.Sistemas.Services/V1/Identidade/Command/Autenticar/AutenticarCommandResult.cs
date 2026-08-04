using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Identidade.Command.Autenticar;

public sealed record AutenticarCommandResult
{
    public bool Sucesso { get; init; }

    public string? Token { get; init; }

    public DateTimeOffset? ExpiraEm { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static AutenticarCommandResult Autenticado(string token, DateTimeOffset expiraEm) => new()
    {
        Sucesso = true,
        Token = token,
        ExpiraEm = expiraEm,
    };

    public static AutenticarCommandResult Recusado(Error erro) => new()
    {
        Sucesso = false,
        Erros = [erro],
    };
}
