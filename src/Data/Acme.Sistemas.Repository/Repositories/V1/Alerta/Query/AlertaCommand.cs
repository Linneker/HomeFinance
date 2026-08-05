namespace Acme.Sistemas.Repository.Repositories.V1.Alerta.Query;

internal static class AlertaCommand
{
    /// <summary>
    /// Emite o alerta. O <c>DO NOTHING</c> se apoia no índice parcial
    /// <c>ux_alerta_condicao_aberta</c>, que só cobre os abertos.
    /// </summary>
    /// <remarks>
    /// É a última barreira contra duplicidade, não a primeira: quem decide se emite é o serviço,
    /// olhando também os dispensados. O índice cobre a corrida entre duas gravações
    /// simultâneas, que a verificação em memória não alcança.
    /// </remarks>
    public const string Gravar = """
        INSERT INTO alerta
            (id, tenant_id, tipo, chave_condicao, estado, mensagem, criado_em)
        VALUES
            (@id, @tenantId, @tipo, @chaveCondicao, @estado, @mensagem, @criadoEm)
        ON CONFLICT DO NOTHING
        """;

    public const string AtualizarEstado = """
        UPDATE alerta
        SET estado        = @estado,
            encerrado_em  = @encerradoEm,
            dispensado_em = @dispensadoEm
        WHERE id = @id
        """;
}
