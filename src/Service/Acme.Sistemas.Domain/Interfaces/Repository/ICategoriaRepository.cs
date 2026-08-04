using Acme.Sistemas.Domain.Entities.Lancamentos;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>Leitura de categoria para as Queries do usuário.</summary>
public interface ICategoriaReadRepository
{
    Task<IReadOnlyList<CategoriaEntity>> ListarAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Escrita de categoria e as leituras que a decidem, todas pelo primário.
/// </summary>
/// <remarks>
/// <see cref="ExisteNomeAsync"/> e <see cref="ExisteAsync"/> vivem aqui porque decidem uma
/// escrita: verificar contra réplica atrasada deixaria passar categoria duplicada e aceitaria
/// lançamento apontando para categoria recém-removida.
/// </remarks>
public interface ICategoriaWriteRepository
{
    Task<bool> ExisteNomeAsync(string nome, CancellationToken cancellationToken);

    /// <summary>Confirma que a categoria existe no tenant ativo antes de vinculá-la.</summary>
    Task<bool> ExisteAsync(Guid categoriaId, CancellationToken cancellationToken);

    Task CriarAsync(CategoriaEntity categoria, CancellationToken cancellationToken);
}
