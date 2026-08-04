using Acme.Sistemas.Domain.Entities.Lancamentos;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

public interface IRecorrenciaReadRepository
{
    Task<IReadOnlyList<RecorrenciaEntity>> ListarAtivasAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Escrita de recorrência e a materialização das ocorrências previstas.
/// </summary>
/// <remarks>
/// A materialização é idempotente: apaga as ocorrências futuras ainda não confirmadas e
/// regrava. Ocorrência já realizada nunca é tocada — reprocessar a regra não pode reescrever
/// o passado do cliente.
/// </remarks>
public interface IRecorrenciaWriteRepository
{
    Task<RecorrenciaEntity?> ObterAsync(Guid recorrenciaId, CancellationToken cancellationToken);

    /// <summary>Cria a regra e materializa o horizonte, numa transação só.</summary>
    Task CriarAsync(
        RecorrenciaEntity recorrencia,
        IReadOnlyList<LancamentoEntity> ocorrencias,
        IReadOnlyList<LancamentoParticipacaoEntity> participacoes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Encerra a regra e remove as ocorrências previstas a partir do corte. Devolve falso se
    /// não havia recorrência ativa — chamar de novo não encerra nada nem apaga mais nada.
    /// </summary>
    Task<bool> EncerrarAsync(
        Guid recorrenciaId,
        DateOnly aPartirDe,
        DateTimeOffset encerradaEm,
        CancellationToken cancellationToken);
}
