using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Identidade.Query.ConsentimentoVigente;

public static class ConsentimentoVigenteQueryValidation
{
    public const string CodigoVersaoObrigatoria = "versao_obrigatoria";

    public static IReadOnlyList<Error> Validar(ConsentimentoVigenteQuery query) =>
        string.IsNullOrWhiteSpace(query.VersaoAtual)
            ? [new Error(CodigoVersaoObrigatoria, "Informe a versão vigente dos documentos.")]
            : [];
}
