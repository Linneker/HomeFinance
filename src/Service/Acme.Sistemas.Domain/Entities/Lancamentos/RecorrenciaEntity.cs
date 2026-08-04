namespace Acme.Sistemas.Domain.Entities.Lancamentos;

/// <summary>
/// Regra de repetição mensal. As ocorrências previstas são materializadas a partir dela.
/// </summary>
/// <remarks>
/// A regra é linha própria, e não inferida do primeiro lançamento do grupo, porque encerrar a
/// recorrência precisa deixar marca: sem <see cref="EncerradaEm"/>, uma nova materialização
/// recriaria as ocorrências que o usuário acabou de encerrar.
/// </remarks>
public sealed class RecorrenciaEntity : TenantEntityBase
{
    public required string Descricao { get; set; }

    public decimal Valor { get; set; }

    /// <summary>
    /// Dia do mês da ocorrência. Em mês mais curto, cai no último dia — dia 31 em fevereiro
    /// vira 28 ou 29, e não deixa de existir.
    /// </summary>
    public int DiaDoMes { get; set; }

    public Guid? CategoriaId { get; set; }

    public DateOnly Inicio { get; set; }

    public DateTimeOffset? EncerradaEm { get; set; }

    public bool Ativa => EncerradaEm is null;

    /// <summary>
    /// Horizonte de materialização. Projetar saldo exige que as saídas futuras existam como
    /// linhas consultáveis; calcular a recorrência sob demanda em toda projeção seria mais
    /// elegante e bem mais caro.
    /// </summary>
    public const int MesesDeHorizonte = 12;
}
