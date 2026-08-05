namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.FonteRenda;

/// <param name="VigenteAPartirDe">
/// Mês em que a renda passa a valer. Nulo usa o mês corrente — é o caso de quem está
/// cadastrando a renda que já recebe hoje, que é a esmagadora maioria.
/// </param>
public sealed record CadastrarFonteRendaRequest(
    string Nome,
    decimal Valor,
    int DiaRecebimento,
    DateOnly? VigenteAPartirDe = null);

/// <param name="VigenteAPartirDe">
/// Mês a partir do qual o novo valor vale. Meses anteriores mantêm o valor antigo. Nulo usa o
/// mês corrente.
/// </param>
public sealed record AlterarFonteRendaRequest(
    string Nome,
    decimal Valor,
    int DiaRecebimento,
    DateOnly? VigenteAPartirDe = null);
