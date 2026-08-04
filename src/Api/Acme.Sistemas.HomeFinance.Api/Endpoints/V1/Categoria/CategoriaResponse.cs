namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Categoria;

public sealed record CategoriaCriadaResponse(Guid CategoriaId);

public sealed record CategoriaItemResponse(Guid Id, string Nome);

public sealed record CategoriasResponse(IReadOnlyList<CategoriaItemResponse> Categorias);
