using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Entities.Users;

namespace Acme.Sistemas.Services.V1.Identidade.Command.RegistrarConsentimento;

public static class RegistrarConsentimentoCommandValidation
{
    public const string CodigoDocumentoDesconhecido = "documento_desconhecido";
    public const string CodigoVersaoObrigatoria = "versao_obrigatoria";

    public static IReadOnlyList<Error> Validar(RegistrarConsentimentoCommand command)
    {
        var erros = new List<Error>();

        // Aceitar documento arbitrário permitiria registrar consentimento de algo que não
        // existe, e a prova do aceite deixaria de significar qualquer coisa.
        if (!DocumentoConsentimento.Obrigatorios.Contains(command.Documento))
        {
            erros.Add(new Error(
                CodigoDocumentoDesconhecido,
                "Documento inválido.",
                nameof(command.Documento)));
        }

        if (string.IsNullOrWhiteSpace(command.Versao))
        {
            erros.Add(new Error(
                CodigoVersaoObrigatoria,
                "A versão do documento é obrigatória.",
                nameof(command.Versao)));
        }

        return erros;
    }
}
