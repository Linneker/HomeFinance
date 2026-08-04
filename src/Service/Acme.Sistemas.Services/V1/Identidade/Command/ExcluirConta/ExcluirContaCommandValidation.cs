using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Identidade.Command.ExcluirConta;

public static class ExcluirContaCommandValidation
{
    public const string CodigoConfirmacaoAusente = "confirmacao_ausente";
    public const string CodigoUsuarioObrigatorio = "usuario_obrigatorio";

    public static IReadOnlyList<Error> Validar(ExcluirContaCommand command)
    {
        var erros = new List<Error>();

        if (command.UsuarioId == Guid.Empty)
        {
            erros.Add(new Error(CodigoUsuarioObrigatorio, "Titular não identificado."));
        }

        if (!command.ConfirmacaoDoTitular)
        {
            erros.Add(new Error(
                CodigoConfirmacaoAusente,
                "É necessário confirmar a exclusão da conta.",
                nameof(command.ConfirmacaoDoTitular)));
        }

        return erros;
    }
}
