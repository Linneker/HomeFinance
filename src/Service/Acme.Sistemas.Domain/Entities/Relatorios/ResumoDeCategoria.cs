namespace Acme.Sistemas.Domain.Entities.Relatorios;

/// <summary>Total apurado de uma categoria no mês, como vem do banco.</summary>
/// <param name="CategoriaId">Nulo agrupa os lançamentos ainda pendentes de classificação.</param>
public sealed record TotalPorCategoria(Guid? CategoriaId, decimal Realizado, int Quantidade);

/// <summary>
/// Uma categoria orçada confrontada com o que foi gasto nela.
/// </summary>
/// <remarks>
/// A linha existe para toda categoria com orçamento definido, mesmo sem gasto nenhum. Omitir a
/// categoria zerada esconderia justamente o que a pessoa planejou e não usou — que é metade da
/// informação de um orçamento.
/// </remarks>
public sealed record LinhaDoResumo(
    Guid CategoriaId,
    decimal Planejado,
    decimal Realizado,
    int Quantidade)
{
    public bool Excedida => Realizado > Planejado;

    /// <summary>Quanto ainda cabe. Zero quando a categoria estourou.</summary>
    public decimal Disponivel => Excedida ? 0m : Planejado - Realizado;

    /// <summary>Quanto passou do planejado. Zero quando está dentro.</summary>
    public decimal Excedente => Excedida ? Realizado - Planejado : 0m;
}

/// <summary>
/// Gastos em categorias sem valor planejado no mês, e os ainda sem categoria.
/// </summary>
/// <remarks>
/// Ficam fora das linhas por categoria de propósito: misturá-los ao planejado faria a soma do
/// realizado parecer coberta por um orçamento que nunca existiu. Aparecem como bloco próprio,
/// que é o que revela quanto do mês escapou do plano.
/// </remarks>
public sealed record ForaDoPlanejamento(
    IReadOnlyList<TotalPorCategoria> Categorias,
    decimal Total,
    decimal PendenteDeClassificacao);
