using Acme.Sistemas.Domain.Entities.Lancamentos;

namespace Acme.Sistemas.Domain.Entities.Planejamento;

/// <summary>
/// Uma entrada de dinheiro declarada pela pessoa, válida por um intervalo de meses.
/// </summary>
/// <remarks>
/// A linha representa uma <em>vigência</em>, não a fonte inteira ao longo do tempo. Alterar o
/// valor a partir de um mês fecha esta vigência e abre outra — é o que faz os meses já
/// conferidos manterem o valor que tinham. Sobrescrever a linha reescreveria o passado: o
/// relatório de um mês fechado passaria a mostrar uma renda que não era a daquele mês.
/// <para>
/// A renda é sempre declarada, nunca inferida de lançamentos. Entrada de dinheiro em conta não
/// distingue salário de transferência entre contas próprias ou de devolução de empréstimo, e
/// errar isso para mais faz o orçamento inteiro parecer folgado.
/// </para>
/// </remarks>
public sealed class FonteRendaEntity : TenantEntityBase
{
    public required string Nome { get; set; }

    public decimal Valor { get; set; }

    /// <summary>Dia do mês em que o dinheiro entra. Alimenta a projeção de caixa.</summary>
    public int DiaRecebimento { get; set; }

    /// <summary>Primeiro dia do primeiro mês em que esta vigência vale.</summary>
    public DateOnly VigenciaInicio { get; set; }

    /// <summary>
    /// Último dia do último mês em que vale. Nulo enquanto a fonte segue ativa — e é o caso
    /// normal, porque salário não tem data de término conhecida.
    /// </summary>
    public DateOnly? VigenciaFim { get; set; }

    public bool Encerrada => VigenciaFim is not null;

    /// <summary>
    /// Vale no mês de <paramref name="competencia"/>?
    /// </summary>
    /// <remarks>
    /// Compara contra o primeiro dia do mês pedido. Comparar contra a data crua faria uma
    /// vigência iniciada em 01/08 parecer inválida para uma competência informada como 15/08.
    /// </remarks>
    public bool VigenteEm(DateOnly competencia)
    {
        var mes = CalendarioFinanceiro.PrimeiroDiaDoMes(competencia);

        return VigenciaInicio <= mes && (VigenciaFim is null || VigenciaFim >= mes);
    }

    /// <summary>Data em que o dinheiro entra no mês pedido, respeitando meses curtos.</summary>
    public DateOnly RecebimentoEm(DateOnly competencia) =>
        CalendarioFinanceiro.NoMes(competencia.Year, competencia.Month, DiaRecebimento);

    /// <summary>
    /// Fecha esta vigência no mês anterior ao da nova, para que as duas não se sobreponham.
    /// </summary>
    /// <remarks>
    /// Sobreposição somaria a mesma fonte duas vezes no mês da virada e a renda prevista
    /// saltaria sem que nada tivesse mudado na vida da pessoa.
    /// </remarks>
    public void EncerrarAntesDe(DateOnly novaVigencia) =>
        VigenciaFim = CalendarioFinanceiro.UltimoDiaDoMesAnterior(novaVigencia);
}
