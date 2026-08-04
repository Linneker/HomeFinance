using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Identidade.Command.Autenticar;

public static class AutenticarCommandValidation
{
    public const string CodigoCampoObrigatorio = "campo_obrigatorio";

    public static IReadOnlyList<Error> Validar(AutenticarCommand command)
    {
        var erros = new List<Error>();

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            erros.Add(new Error(CodigoCampoObrigatorio, "Informe o e-mail.", nameof(command.Email)));
        }

        if (string.IsNullOrWhiteSpace(command.Senha))
        {
            erros.Add(new Error(CodigoCampoObrigatorio, "Informe a senha.", nameof(command.Senha)));
        }

        return erros;
    }
}
