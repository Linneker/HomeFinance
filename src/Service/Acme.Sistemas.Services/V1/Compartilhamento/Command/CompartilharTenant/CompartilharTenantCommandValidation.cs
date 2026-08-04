using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;

public static class CompartilharTenantCommandValidation
{
    public const string CodigoConsentimentoNaoConfirmado = "consentimento_nao_confirmado";
    public const string CodigoEmailObrigatorio = "email_obrigatorio";

    public static IReadOnlyList<Error> Validar(CompartilharTenantCommand command)
    {
        var erros = new List<Error>();

        if (string.IsNullOrWhiteSpace(command.EmailDestinatario))
        {
            erros.Add(new Error(
                CodigoEmailObrigatorio,
                "Informe o e-mail de quem vai receber o acesso.",
                nameof(command.EmailDestinatario)));
        }

        if (!command.ConsentimentoConfirmado)
        {
            erros.Add(new Error(
                CodigoConsentimentoNaoConfirmado,
                "É necessário confirmar o que ficará visível antes de compartilhar.",
                nameof(command.ConsentimentoConfirmado)));
        }

        return erros;
    }
}
