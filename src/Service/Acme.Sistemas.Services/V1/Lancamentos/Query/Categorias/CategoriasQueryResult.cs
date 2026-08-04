namespace Acme.Sistemas.Services.V1.Lancamentos.Query.Categorias;

public sealed record CategoriaListada(Guid Id, string Nome);

public sealed record CategoriasQueryResult(IReadOnlyList<CategoriaListada> Categorias);
