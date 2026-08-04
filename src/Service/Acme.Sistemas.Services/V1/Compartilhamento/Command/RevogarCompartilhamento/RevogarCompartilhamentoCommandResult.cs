using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Command.RevogarCompartilhamento;

public sealed record RevogarCompartilhamentoCommandResult
{
    public bool Sucesso { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static RevogarCompartilhamentoCommandResult Revogado() => new() { Sucesso = true };

    public static RevogarCompartilhamentoCommandResult Recusado(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };
}
