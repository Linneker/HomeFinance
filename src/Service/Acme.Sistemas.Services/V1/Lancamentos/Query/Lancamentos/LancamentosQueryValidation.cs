using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Lancamentos.Query.Lancamentos;

public static class LancamentosQueryValidation
{
    public const string CodigoPeriodoObrigatorio = "periodo_obrigatorio";
    public const string CodigoPeriodoInvertido = "periodo_invertido";

    public static IReadOnlyList<Error> Validar(LancamentosQuery query)
    {
        // Período obrigatório: uma listagem sem recorte percorreria o histórico inteiro do
        // cliente e a consulta deixaria de usar o índice por tenant e data.
        if (query.De is null || query.Ate is null)
        {
            return [new Error(CodigoPeriodoObrigatorio, "Informe o período a consultar.")];
        }

        return query.De > query.Ate
            ? [new Error(CodigoPeriodoInvertido, "A data inicial não pode ser maior que a final.")]
            : [];
    }
}
