namespace Acme.Sistemas.Domain.Entities.Planejamento;

/// <summary>
/// Renda prevista de um mês e o confronto dela com o que foi planejado gastar.
/// </summary>
public static class PrevisaoDeRenda
{
    /// <summary>Soma das fontes vigentes no mês. Zero quando nenhuma vale ali.</summary>
    public static decimal DoMes(DateOnly competencia, IEnumerable<FonteRendaEntity> fontes) =>
        fontes.Where(fonte => fonte.VigenteEm(competencia)).Sum(fonte => fonte.Valor);

    /// <summary>
    /// Compara o total planejado com a renda prevista.
    /// </summary>
    /// <remarks>
    /// Devolve um aviso, não um erro: planejar acima da renda é decisão legítima de quem vai
    /// usar reserva, décimo terceiro ou uma entrada que ainda não cadastrou. Bloquear a
    /// gravação obrigaria a pessoa a mentir no orçamento para conseguir salvá-lo, e o
    /// orçamento mentido não serve para comparar com o gasto real depois.
    /// <para>
    /// Renda zero não gera aviso: quem ainda não declarou renda estaria vendo alerta em toda
    /// linha que digitasse, e o aviso perderia o sentido antes de existir o que avisar. Quem
    /// cobre esse caso é o onboarding, que pede a renda como requisito bloqueante.
    /// </para>
    /// </remarks>
    public static AvisoDeOrcamento? Avaliar(decimal rendaPrevista, decimal totalPlanejado) =>
        rendaPrevista > 0 && totalPlanejado > rendaPrevista
            ? new AvisoDeOrcamento(rendaPrevista, totalPlanejado)
            : null;
}

/// <summary>Sinaliza planejamento acima da renda, sem impedir a operação.</summary>
public sealed record AvisoDeOrcamento(decimal RendaPrevista, decimal TotalPlanejado)
{
    public decimal Excedente => TotalPlanejado - RendaPrevista;

    public string Mensagem =>
        $"O planejado do mês ({TotalPlanejado:N2}) supera a renda prevista " +
        $"({RendaPrevista:N2}) em {Excedente:N2}.";
}
