namespace Acme.Sistemas.Repository.Repositories.V1.FonteRenda.Query;

internal static class FonteRendaCommand
{
    public const string Gravar = """
        INSERT INTO fonte_renda
            (id, tenant_id, nome, valor, dia_recebimento, vigencia_inicio, vigencia_fim, criado_em)
        VALUES
            (@id, @tenantId, @nome, @valor, @diaRecebimento, @vigenciaInicio, @vigenciaFim, @criadoEm)
        """;

    /// <summary>
    /// Altera a vigência corrente sem criar sucessora. Só vale para correção do mês em que a
    /// própria vigência começou — fora disso a alteração passa por <c>Encerrar</c> + <c>Gravar</c>.
    /// </summary>
    public const string Alterar = """
        UPDATE fonte_renda
        SET nome            = @nome,
            valor           = @valor,
            dia_recebimento = @diaRecebimento
        WHERE id = @id
        """;

    public const string Encerrar = """
        UPDATE fonte_renda
        SET vigencia_fim = @vigenciaFim
        WHERE id = @id
        """;

    public const string Remover = """
        DELETE FROM fonte_renda
        WHERE id = @id
        """;
}
