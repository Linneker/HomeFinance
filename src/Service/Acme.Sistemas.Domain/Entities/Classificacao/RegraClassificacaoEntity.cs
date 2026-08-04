namespace Acme.Sistemas.Domain.Entities.Classificacao;

/// <summary>
/// Associa um trecho de descrição a uma categoria, para não classificar a mesma compra duas vezes.
/// </summary>
/// <remarks>
/// O padrão é guardado já normalizado — é o que permite casá-lo por índice em vez de
/// normalizar dentro da consulta.
/// </remarks>
public sealed class RegraClassificacaoEntity : TenantEntityBase
{
    /// <summary>Trecho procurado na descrição, já em forma canônica.</summary>
    public required string Padrao { get; set; }

    public Guid CategoriaId { get; set; }

    /// <summary>
    /// Casa quando o padrão aparece em qualquer ponto da descrição. Contém, e não igual: a
    /// fatura acrescenta prefixo de adquirente e sufixo de estabelecimento à mesma loja.
    /// </summary>
    public bool Casa(string descricaoNormalizada) =>
        Padrao.Length > 0 && descricaoNormalizada.Contains(Padrao, StringComparison.Ordinal);

    /// <summary>
    /// Quanto mais longo o padrão, mais específica a regra.
    /// </summary>
    /// <remarks>
    /// Não é uma métrica sofisticada, e é a certa aqui: entre "IFOOD" e "IFOOD MERCADO", a
    /// segunda descreve um caso mais estreito e deve ganhar. Empate resolve pela mais recente,
    /// porque a regra criada depois reflete a decisão mais atual da pessoa.
    /// </remarks>
    public int Especificidade => Padrao.Length;
}
