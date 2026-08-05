namespace Acme.Sistemas.Repository.Repositories.V1.DocumentoRetido.Query;

internal static class DocumentoRetidoQuery
{
    /// <summary>Ordem de chegada: a fila retida é processada como fila.</summary>
    public const string Pendentes = """
        SELECT id, tenant_id, referencia, tipo, recebido_em, processado_em
        FROM documento_retido
        WHERE processado_em IS NULL
        ORDER BY recebido_em
        """;
}
