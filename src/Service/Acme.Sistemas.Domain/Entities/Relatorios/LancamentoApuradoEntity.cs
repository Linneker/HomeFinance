using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Domain.Entities.Relatorios;

/// <summary>
/// Linha do extrato, com o valor já apurado a partir das participações.
/// </summary>
/// <remarks>
/// <see cref="ValorApurado"/> e <see cref="Valor"/> são iguais nesta fase, em que toda
/// participação é de 100%, e é justamente por isso que precisam ser campos distintos desde já:
/// quando a divisão entre pessoas entrar, o relatório passa a somar o apurado sem que nenhuma
/// consulta precise ser reescrita. Somar <see cref="Valor"/> hoje criaria a dívida de reescrever
/// todo relatório depois — e de descobrir isso em produção, quando os números não fecharem.
/// </remarks>
public sealed class LancamentoApuradoEntity : TenantEntityBase
{
    public required string Descricao { get; set; }

    /// <summary>Valor cheio do lançamento, como digitado.</summary>
    public decimal Valor { get; set; }

    /// <summary>Parcela do valor que cabe a este tenant, somadas as participações.</summary>
    public decimal ValorApurado { get; set; }

    public DateOnly DataCompetencia { get; set; }

    public DateOnly DataCaixa { get; set; }

    public Guid? CategoriaId { get; set; }

    public Guid? GrupoParcelamentoId { get; set; }

    public int? ParcelaNumero { get; set; }

    public int? ParcelaTotal { get; set; }

    public Guid? RecorrenciaId { get; set; }

    /// <summary>Ocorrência futura ainda não confirmada. Entra na projeção, não no realizado.</summary>
    public bool Previsto { get; set; }

    public bool PendenteDeClassificacao => CategoriaId is null;

    public TipoLancamento Tipo => GrupoParcelamentoId is not null
        ? TipoLancamento.Parcela
        : RecorrenciaId is not null
            ? TipoLancamento.Recorrente
            : TipoLancamento.Avulso;

    /// <summary>Rótulo "3/10" para a tela. Vazio quando não é parcela.</summary>
    public string? Parcelamento => ParcelaNumero is null || ParcelaTotal is null
        ? null
        : $"{ParcelaNumero}/{ParcelaTotal}";
}
