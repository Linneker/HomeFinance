using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Command.RevogarCompartilhamento;

public static class RevogarCompartilhamentoCommandValidation
{
    public const string CodigoDestinatarioObrigatorio = "destinatario_obrigatorio";

    public static IReadOnlyList<Error> Validar(RevogarCompartilhamentoCommand command) =>
        command.UsuarioDestinatarioId == Guid.Empty
            ? [new Error(
                CodigoDestinatarioObrigatorio,
                "Informe de quem o acesso será revogado.",
                nameof(command.UsuarioDestinatarioId))]
            : [];
}
