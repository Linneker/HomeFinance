using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Administracao.Command.AutenticarAdministrador;

public static class AutenticarAdministradorCommandValidation
{
    public const string CodigoCredencialObrigatoria = "credencial_obrigatoria";

    public static IReadOnlyList<Error> Validar(AutenticarAdministradorCommand command)
    {
        var erros = new List<Error>();

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            erros.Add(new Error(CodigoCredencialObrigatoria, "Informe o e-mail.", nameof(command.Email)));
        }

        if (string.IsNullOrWhiteSpace(command.Senha))
        {
            erros.Add(new Error(CodigoCredencialObrigatoria, "Informe a senha.", nameof(command.Senha)));
        }

        return erros;
    }
}
