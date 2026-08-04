using Acme.Sistemas.Domain.Entities.Classificacao;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>Leitura das regras para a tela de gestão.</summary>
public interface IRegraClassificacaoReadRepository
{
    Task<IReadOnlyList<RegraClassificacaoEntity>> ListarAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Escrita das regras e as leituras que a decidem, todas pelo primário.
/// </summary>
/// <remarks>
/// <see cref="ListarAsync"/> vive aqui, e não no repositório de leitura, porque é consumida ao
/// gravar um lançamento: é a regra recém-criada pela pessoa que precisa valer no lançamento
/// seguinte, e contra réplica atrasada ela não estaria lá — o lançamento cairia em pendente e
/// a correção manual pareceria não ter surtido efeito.
/// </remarks>
public interface IRegraClassificacaoWriteRepository
{
    Task<IReadOnlyList<RegraClassificacaoEntity>> ListarAsync(CancellationToken cancellationToken);

    Task<RegraClassificacaoEntity?> ObterAsync(Guid regraId, CancellationToken cancellationToken);

    /// <summary>
    /// Grava a regra, substituindo a categoria se já houver regra para o mesmo padrão.
    /// </summary>
    /// <remarks>
    /// É substituição e não erro de duplicidade: corrigir a categoria de um lançamento e
    /// aceitar criar a regra significa que a associação anterior daquele padrão está errada.
    /// </remarks>
    Task GravarAsync(RegraClassificacaoEntity regra, CancellationToken cancellationToken);

    Task<bool> AlterarAsync(RegraClassificacaoEntity regra, CancellationToken cancellationToken);

    Task<bool> RemoverAsync(Guid regraId, CancellationToken cancellationToken);
}
