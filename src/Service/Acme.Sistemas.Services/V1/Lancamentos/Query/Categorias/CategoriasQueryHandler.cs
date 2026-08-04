using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Lancamentos.Query.Categorias;

public sealed class CategoriasQueryHandler(ICategoriaReadRepository categorias)
    : IRequestHandler<CategoriasQuery, CategoriasQueryResult>
{
    public async Task<CategoriasQueryResult> HandleAsync(
        CategoriasQuery request,
        CancellationToken cancellationToken)
    {
        if (CategoriasQueryValidation.Validar(request).Count > 0)
        {
            return new CategoriasQueryResult([]);
        }

        var encontradas = await categorias.ListarAsync(cancellationToken);

        return new CategoriasQueryResult(
            [.. encontradas.Select(categoria => new CategoriaListada(categoria.Id, categoria.Nome))]);
    }
}
