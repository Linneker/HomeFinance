namespace Acme.Sistemas.Repository.Repositories.V1.DocumentoRetido.Query;

internal static class DocumentoRetidoCommand
{
    public const string Reter = """
        INSERT INTO documento_retido (id, tenant_id, referencia, tipo, recebido_em)
        VALUES (@id, @tenantId, @referencia, @tipo, @recebidoEm)
        """;

    /// <summary>
    /// Marca a fila inteira de uma vez.
    /// </summary>
    /// <remarks>
    /// <c>= ANY(@ids)</c> e não um <c>IN</c> montado por concatenação: a lista vem de uma
    /// consulta e pode ter qualquer tamanho, e concatenar identificadores em SQL é injeção
    /// mesmo quando a origem parece confiável hoje.
    /// <para>
    /// O <c>processado_em IS NULL</c> no filtro mantém a operação idempotente: rodar a fila duas
    /// vezes não move a data do primeiro processamento.
    /// </para>
    /// </remarks>
    public const string MarcarProcessados = """
        UPDATE documento_retido
        SET processado_em = @processadoEm
        WHERE id = ANY(@ids)
          AND processado_em IS NULL
        """;
}
