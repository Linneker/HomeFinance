using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Classificacao.Command.RemoverRegra;

public sealed record RemoverRegraCommandResult
{
    public bool Sucesso { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static RemoverRegraCommandResult Removida() => new() { Sucesso = true };

    public static RemoverRegraCommandResult Recusada(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };
}
