using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Domain.Entities.Lancamentos;

/// <summary>
/// Unidade canônica de todo movimento financeiro.
/// </summary>
/// <remarks>
/// As duas datas são obrigatórias sempre. <see cref="DataCompetencia"/> é quando o gasto
/// aconteceu; <see cref="DataCaixa"/> é quando o dinheiro sai. Guardar só uma inviabilizaria a
/// outra visão sem reprocessar o histórico inteiro dos clientes.
/// <para>
/// Parcela é lançamento irmão, não filho: a compra em três vezes vira três linhas com a mesma
/// competência e caixas distintos, ligadas por <see cref="GrupoParcelamentoId"/>. As duas
/// visões passam a ser o mesmo agrupamento sobre colunas diferentes, sem nenhum relatório
/// precisar saber que tipo de linha está somando.
/// </para>
/// </remarks>
public sealed class LancamentoEntity : TenantEntityBase
{
    public required string Descricao { get; set; }

    public decimal Valor { get; set; }

    /// <summary>Quando o gasto aconteceu. Visão primária do produto.</summary>
    public DateOnly DataCompetencia { get; set; }

    /// <summary>Quando o dinheiro sai. Sustenta a projeção de saldo.</summary>
    public DateOnly DataCaixa { get; set; }

    public Guid? CategoriaId { get; set; }

    public OrigemClassificacao OrigemClassificacao { get; set; }

    public Guid? GrupoParcelamentoId { get; set; }

    public int? ParcelaNumero { get; set; }

    public int? ParcelaTotal { get; set; }

    public Guid? RecorrenciaId { get; set; }

    /// <summary>Ocorrência futura ainda não confirmada. Entra na projeção, não no realizado.</summary>
    public bool Previsto { get; set; }

    public DateTimeOffset? AlteradoEm { get; set; }

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
