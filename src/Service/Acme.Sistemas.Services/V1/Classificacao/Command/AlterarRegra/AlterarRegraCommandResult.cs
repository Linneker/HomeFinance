using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Classificacao.Command.AlterarRegra;

public sealed record AlterarRegraCommandResult
{
    public bool Sucesso { get; init; }

    /// <summary>Padrão como ficou gravado, já normalizado.</summary>
    public string? Padrao { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static AlterarRegraCommandResult Alterada(string padrao) =>
        new() { Sucesso = true, Padrao = padrao };

    public static AlterarRegraCommandResult Recusada(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };
}
