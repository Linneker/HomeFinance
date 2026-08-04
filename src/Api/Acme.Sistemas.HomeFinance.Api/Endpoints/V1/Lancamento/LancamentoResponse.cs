namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Lancamento;

/// <param name="LancamentoIds">Mais de um em compra parcelada: as parcelas são linhas irmãs.</param>
public sealed record LancamentoCriadoResponse(IReadOnlyList<Guid> LancamentoIds);

/// <param name="Semelhantes">Quantos lançamentos iguais já existem no mês de competência.</param>
public sealed record PossivelDuplicidadeResponse(int Semelhantes, string Mensagem);

public sealed record LancamentoItemResponse(
    Guid Id,
    string Descricao,
    decimal Valor,
    DateOnly DataCompetencia,
    DateOnly DataCaixa,
    Guid? CategoriaId,
    bool PendenteDeClassificacao,
    string Tipo,
    string? Parcelamento,
    bool Previsto);

/// <param name="VisaoPorCaixa">
/// A tela precisa rotular qual visão está exibindo: dois totais diferentes para o mesmo mês,
/// sem rótulo, parecem erro do sistema.
/// </param>
public sealed record LancamentosResponse(
    IReadOnlyList<LancamentoItemResponse> Lancamentos,
    bool VisaoPorCaixa,
    decimal Total);

public sealed record LancamentoAlteradoResponse(IReadOnlyList<DateOnly> CompetenciasAfetadas);
