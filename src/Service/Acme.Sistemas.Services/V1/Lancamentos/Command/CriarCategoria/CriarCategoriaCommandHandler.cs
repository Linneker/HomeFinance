using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.CriarCategoria;

/// <summary>
/// Cria uma categoria no tenant ativo.
/// </summary>
/// <remarks>
/// A verificação de nome repetido usa o repositório de escrita: decide a gravação, e contra
/// réplica atrasada duas categorias iguais entrariam e quebrariam o resumo por categoria em
/// duas linhas com o mesmo rótulo.
/// </remarks>
public sealed class CriarCategoriaCommandHandler(
    ICategoriaWriteRepository categorias,
    TimeProvider relogio)
    : IRequestHandler<CriarCategoriaCommand, CriarCategoriaCommandResult>
{
    public const string CodigoNomeEmUso = "categoria_ja_existe";

    public async Task<CriarCategoriaCommandResult> HandleAsync(
        CriarCategoriaCommand request,
        CancellationToken cancellationToken)
    {
        var erros = CriarCategoriaCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return CriarCategoriaCommandResult.Recusada(erros[0]);
        }

        var nome = request.Nome.Trim();

        if (await categorias.ExisteNomeAsync(nome, cancellationToken))
        {
            return CriarCategoriaCommandResult.Recusada(
                new Error(CodigoNomeEmUso, "Já existe uma categoria com este nome.", nameof(request.Nome)));
        }

        var categoria = new CategoriaEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = request.TenantId,
            Nome = nome,
            CriadoEm = relogio.GetUtcNow(),
        };

        await categorias.CriarAsync(categoria, cancellationToken);

        return CriarCategoriaCommandResult.Criada(categoria.Id);
    }
}
