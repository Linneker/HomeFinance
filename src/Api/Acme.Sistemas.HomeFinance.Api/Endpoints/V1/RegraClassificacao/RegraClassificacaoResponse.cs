namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.RegraClassificacao;

public sealed record RegraItemResponse(
    Guid Id,
    string Padrao,
    Guid CategoriaId,
    DateTimeOffset CriadaEm);

/// <param name="Regras">
/// Na ordem de precedência em que são aplicadas — é o que explica por que uma compra caiu numa
/// categoria e não em outra quando duas regras casam.
/// </param>
public sealed record RegrasResponse(IReadOnlyList<RegraItemResponse> Regras);

public sealed record RegraAlteradaResponse(string Padrao);
