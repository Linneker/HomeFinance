using Acme.Sistemas.Domain.Entities.Assinaturas;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>
/// Fila de documentos retidos do tenant ativo.
/// </summary>
/// <remarks>
/// Sem separação leitura/escrita: as duas leituras aqui decidem escrita — a fila é lida para ser
/// marcada como processada, e uma réplica atrasada reprocessaria o que já saiu dela.
/// </remarks>
public interface IDocumentoRetidoRepository
{
    Task ReterAsync(DocumentoRetidoEntity documento, CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentoRetidoEntity>> PendentesAsync(CancellationToken cancellationToken);

    Task<int> MarcarProcessadosAsync(
        IReadOnlyList<Guid> documentoIds,
        DateTimeOffset quando,
        CancellationToken cancellationToken);
}
