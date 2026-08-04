using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Services.V1.Lancamentos.Query.Lancamentos;

/// <param name="Parcelamento">Rótulo "3/10". Nulo quando não é parcela.</param>
/// <param name="Previsto">Ocorrência futura de recorrência, ainda não confirmada.</param>
public sealed record LancamentoListado(
    Guid Id,
    string Descricao,
    decimal Valor,
    DateOnly DataCompetencia,
    DateOnly DataCaixa,
    Guid? CategoriaId,
    bool PendenteDeClassificacao,
    TipoLancamento Tipo,
    string? Parcelamento,
    bool Previsto);

/// <param name="VisaoPorCaixa">
/// Sempre explícito no resultado. Sem isto a tela não teria como rotular qual visão está
/// exibindo, e dois números diferentes para o mesmo mês pareceriam erro do sistema.
/// </param>
public sealed record LancamentosQueryResult(
    IReadOnlyList<LancamentoListado> Lancamentos,
    bool VisaoPorCaixa)
{
    public IReadOnlyList<Error> Erros { get; init; } = [];

    public decimal Total => Lancamentos.Sum(lancamento => lancamento.Valor);

    public static LancamentosQueryResult Recusada(Error erro) =>
        new([], false) { Erros = [erro] };
}
