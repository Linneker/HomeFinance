namespace Acme.Sistemas.Domain.Entities.Lancamentos;

/// <summary>
/// Divisão do valor de uma compra parcelada.
/// </summary>
/// <remarks>
/// A soma das parcelas tem que devolver o total exato: a competência soma o valor integral no
/// mês da compra, e um centavo perdido no arredondamento apareceria como diferença
/// inexplicável entre o extrato e a fatura. A sobra vai na primeira parcela, como fazem as
/// operadoras de cartão.
/// </remarks>
public static class RateioDeParcelas
{
    public static IReadOnlyList<decimal> Dividir(decimal total, int parcelas)
    {
        if (parcelas <= 1)
        {
            return [total];
        }

        var valorBase = Math.Round(total / parcelas, 2, MidpointRounding.AwayFromZero);
        var sobra = total - (valorBase * parcelas);

        return [.. Enumerable.Range(0, parcelas)
            .Select(indice => indice == 0 ? valorBase + sobra : valorBase)];
    }
}
