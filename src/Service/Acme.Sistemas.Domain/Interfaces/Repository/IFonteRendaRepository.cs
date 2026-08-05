using Acme.Sistemas.Domain.Entities.Planejamento;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>Leitura das fontes de renda para a tela de planejamento.</summary>
public interface IFonteRendaReadRepository
{
    /// <summary>Fontes vigentes no mês pedido, ou todas quando <c>null</c>.</summary>
    Task<IReadOnlyList<FonteRendaEntity>> ListarAsync(
        DateOnly? competencia,
        CancellationToken cancellationToken);
}

/// <summary>
/// Escrita das fontes e as leituras que decidem essa escrita, todas pelo primário.
/// </summary>
/// <remarks>
/// <see cref="ListarAsync"/> vive aqui porque o aviso de orçamento acima da renda é calculado
/// no mesmo Command que grava o orçamento: ler a renda de uma réplica atrasada faria o aviso
/// se basear numa renda que a pessoa acabou de corrigir.
/// </remarks>
public interface IFonteRendaWriteRepository
{
    Task<IReadOnlyList<FonteRendaEntity>> ListarAsync(
        DateOnly? competencia,
        CancellationToken cancellationToken);

    Task<FonteRendaEntity?> ObterAsync(Guid fonteId, CancellationToken cancellationToken);

    Task GravarAsync(FonteRendaEntity fonte, CancellationToken cancellationToken);

    /// <summary>
    /// Fecha a vigência atual e abre a nova numa transação só.
    /// </summary>
    /// <remarks>
    /// Duas chamadas separadas deixariam uma janela em que a fonte antiga já foi encerrada e a
    /// nova ainda não existe — quem consultasse a renda nesse instante veria menos do que a
    /// pessoa ganha, e um alerta de orçamento estourado poderia disparar por isso.
    /// </remarks>
    Task SucederAsync(
        FonteRendaEntity encerrada,
        FonteRendaEntity nova,
        CancellationToken cancellationToken);

    Task<bool> AlterarAsync(FonteRendaEntity fonte, CancellationToken cancellationToken);

    Task<bool> RemoverAsync(Guid fonteId, CancellationToken cancellationToken);
}
