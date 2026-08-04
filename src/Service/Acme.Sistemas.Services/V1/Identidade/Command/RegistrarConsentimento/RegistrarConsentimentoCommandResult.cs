using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Identidade.Command.RegistrarConsentimento;

public sealed record RegistrarConsentimentoCommandResult
{
    public bool Sucesso { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static RegistrarConsentimentoCommandResult Registrado() => new() { Sucesso = true };

    public static RegistrarConsentimentoCommandResult Recusado(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };
}
