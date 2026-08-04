using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.EncerrarRecorrencia;

public static class EncerrarRecorrenciaCommandValidation
{
    public const string CodigoRecorrenciaObrigatoria = "recorrencia_obrigatoria";

    public static IReadOnlyList<Error> Validar(EncerrarRecorrenciaCommand command) =>
        command.RecorrenciaId == Guid.Empty
            ? [new Error(
                CodigoRecorrenciaObrigatoria,
                "Recorrência não identificada.",
                nameof(command.RecorrenciaId))]
            : [];
}
