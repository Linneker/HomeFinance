namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Lancamento;

/// <param name="DataCompetencia">Quando o gasto aconteceu.</param>
/// <param name="DataCaixa">Quando o dinheiro sai. Em compra parcelada, a primeira parcela.</param>
/// <param name="ConfirmouDuplicidade">
/// Reenvio após o aviso de possível duplicidade. As duas datas são anuláveis de propósito:
/// data ausente precisa virar recusa dizendo qual falta, e não um padrão silencioso.
/// </param>
public sealed record CriarLancamentoRequest(
    string Descricao,
    decimal Valor,
    DateOnly? DataCompetencia,
    DateOnly? DataCaixa,
    Guid? CategoriaId,
    int Parcelas = 1,
    bool ConfirmouDuplicidade = false);

public sealed record AlterarLancamentoRequest(
    string Descricao,
    decimal Valor,
    DateOnly? DataCompetencia,
    DateOnly? DataCaixa,
    Guid? CategoriaId);
