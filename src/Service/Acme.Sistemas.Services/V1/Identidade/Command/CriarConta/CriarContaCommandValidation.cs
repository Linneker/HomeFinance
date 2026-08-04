using System.Net.Mail;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Core.Security;

namespace Acme.Sistemas.Services.V1.Identidade.Command.CriarConta;

/// <summary>Validação de entrada — não consulta o banco.</summary>
public static class CriarContaCommandValidation
{
    public const string CodigoEmailInvalido = "email_invalido";
    public const string CodigoSenhaFraca = "senha_fraca";
    public const string CodigoTermosNaoAceitos = "termos_nao_aceitos";

    public static IReadOnlyList<Error> Validar(CriarContaCommand command)
    {
        var erros = new List<Error>();

        if (!EmailValido(command.Email))
        {
            erros.Add(new Error(CodigoEmailInvalido, "Informe um e-mail válido.", nameof(command.Email)));
        }

        foreach (var criterio in PasswordHelper.CriteriosNaoAtendidos(command.Senha))
        {
            erros.Add(new Error(CodigoSenhaFraca, criterio, nameof(command.Senha)));
        }

        if (!command.AceitouTermos || string.IsNullOrWhiteSpace(command.VersaoDocumentos))
        {
            erros.Add(new Error(
                CodigoTermosNaoAceitos,
                "É necessário aceitar os termos de uso e a política de privacidade.",
                nameof(command.AceitouTermos)));
        }

        return erros;
    }

    private static bool EmailValido(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return MailAddress.TryCreate(email, out var endereco)
               && endereco.Host.Contains('.', StringComparison.Ordinal);
    }
}
