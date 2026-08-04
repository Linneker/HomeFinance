namespace Acme.Sistemas.Services.V1.Classificacao.Query.Regras;

/// <param name="Padrao">Já normalizado — é assim que ele casa, e é assim que a tela deve exibir.</param>
public sealed record RegraListada(Guid Id, string Padrao, Guid CategoriaId, DateTimeOffset CriadaEm);

public sealed record RegrasQueryResult(IReadOnlyList<RegraListada> Regras);
