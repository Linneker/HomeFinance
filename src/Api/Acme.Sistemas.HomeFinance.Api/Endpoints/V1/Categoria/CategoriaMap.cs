using Acme.Sistemas.Services.V1.Lancamentos.Command.CriarCategoria;
using Acme.Sistemas.Services.V1.Lancamentos.Query.Categorias;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Categoria;

internal static class CategoriaMap
{
    public static CriarCategoriaCommand ParaCommand(this CategoriaRequest request, Guid tenantId) =>
        new(tenantId, request.Nome);

    public static CategoriaCriadaResponse ParaResponse(this CriarCategoriaCommandResult resultado) =>
        new(resultado.CategoriaId!.Value);

    public static CategoriasResponse ParaResponse(this CategoriasQueryResult resultado) =>
        new([.. resultado.Categorias.Select(c => new CategoriaItemResponse(c.Id, c.Nome))]);
}
