using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.RemoverFonteRenda;

public static class RemoverFonteRendaCommandValidation
{
    public const string CodigoFonteObrigatoria = "fonte_renda_obrigatoria";

    public static IReadOnlyList<Error> Validar(RemoverFonteRendaCommand command) =>
        command.FonteId == Guid.Empty
            ? [new Error(
                CodigoFonteObrigatoria,
                "Fonte de renda não identificada.",
                nameof(command.FonteId))]
            : [];
}
