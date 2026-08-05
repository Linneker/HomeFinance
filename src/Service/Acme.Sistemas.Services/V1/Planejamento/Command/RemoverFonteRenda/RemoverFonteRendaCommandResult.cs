using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.RemoverFonteRenda;

public sealed record RemoverFonteRendaCommandResult
{
    public bool Sucesso { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static RemoverFonteRendaCommandResult Removida() => new() { Sucesso = true };

    public static RemoverFonteRendaCommandResult Recusada(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };
}
