namespace Acme.Sistemas.Domain.Entities.Lancamentos;

/// <summary>
/// Datas mensais de parcelamento e de recorrência.
/// </summary>
/// <remarks>
/// Existe para tornar impossível o erro que parcelamento e recorrência convidam: somar um mês
/// de cada vez a partir da data anterior. <c>AddMonths</c> encurta a data quando o mês de
/// destino é mais curto — 31/01 vira 28/02 — e, iterando, o dia 31 nunca mais volta: a série
/// inteira ficaria presa no dia 28. Contando sempre a partir da origem, março volta ao dia 31.
/// </remarks>
public static class CalendarioFinanceiro
{
    /// <summary>Série mensal a partir da primeira data, sempre contada da origem.</summary>
    public static IReadOnlyList<DateOnly> SerieMensal(DateOnly primeira, int quantidade) =>
        [.. Enumerable.Range(0, quantidade).Select(indice => primeira.AddMonths(indice))];

    /// <summary>
    /// Primeira ocorrência de uma recorrência: o dia escolhido, no mês do início, ou no mês
    /// seguinte se aquele dia já passou.
    /// </summary>
    public static DateOnly PrimeiraOcorrencia(DateOnly inicio, int diaDoMes)
    {
        var noMesDoInicio = NoMes(inicio.Year, inicio.Month, diaDoMes);

        if (noMesDoInicio >= inicio)
        {
            return noMesDoInicio;
        }

        var mesSeguinte = new DateOnly(inicio.Year, inicio.Month, 1).AddMonths(1);

        return NoMes(mesSeguinte.Year, mesSeguinte.Month, diaDoMes);
    }

    /// <summary>O dia pedido, ou o último do mês quando o mês é mais curto.</summary>
    public static DateOnly NoMes(int ano, int mes, int dia) =>
        new(ano, mes, Math.Min(dia, DateTime.DaysInMonth(ano, mes)));
}
