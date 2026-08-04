using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Identidade.Command.ExcluirConta;

public sealed record ExcluirContaCommandResult
{
    public bool Sucesso { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static ExcluirContaCommandResult Excluida() => new() { Sucesso = true };

    public static ExcluirContaCommandResult Recusada(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };
}
