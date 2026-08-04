namespace Acme.Sistemas.Domain.Enums;

/// <summary>
/// Como o lançamento nasceu. Serve à exibição — "3/10", "mensal" — e não à agregação.
/// </summary>
/// <remarks>
/// Não é coluna: é derivado de <c>grupo_parcelamento_id</c> e <c>recorrencia_id</c>. Persistir
/// o tipo criaria uma segunda verdade que pode divergir das colunas que realmente ligam as
/// linhas, e tentaria relatórios a somar de forma diferente conforme o tipo — exatamente o que
/// a decisão de tratar parcela como lançamento irmão evita.
/// </remarks>
public enum TipoLancamento
{
    /// <summary>Movimento único.</summary>
    Avulso = 0,

    /// <summary>Uma das parcelas de uma compra parcelada.</summary>
    Parcela = 1,

    /// <summary>Ocorrência gerada por uma recorrência mensal.</summary>
    Recorrente = 2,
}
