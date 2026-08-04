using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Classificacao.Command.RemoverRegra;

public static class RemoverRegraCommandValidation
{
    public const string CodigoRegraObrigatoria = "regra_obrigatoria";

    public static IReadOnlyList<Error> Validar(RemoverRegraCommand command) =>
        command.RegraId == Guid.Empty
            ? [new Error(CodigoRegraObrigatoria, "Regra não identificada.", nameof(command.RegraId))]
            : [];
}
