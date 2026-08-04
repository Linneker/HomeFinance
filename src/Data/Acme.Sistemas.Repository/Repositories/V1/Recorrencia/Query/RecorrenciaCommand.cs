namespace Acme.Sistemas.Repository.Repositories.V1.Recorrencia.Query;

internal static class RecorrenciaCommand
{
    public const string Inserir = """
        INSERT INTO recorrencia
            (id, tenant_id, descricao, valor, dia_do_mes, categoria_id, inicio, criado_em)
        VALUES (@id, @tenantId, @descricao, @valor, @diaDoMes, @categoriaId, @inicio, @criadoEm)
        """;

    /// <summary>
    /// Só encerra o que está ativo. Chamar de novo devolve zero linhas afetadas, o que torna
    /// o encerramento idempotente sem precisar consultar antes.
    /// </summary>
    public const string Encerrar = """
        UPDATE recorrencia
        SET encerrada_em = @encerradaEm
        WHERE id = @id AND encerrada_em IS NULL
        """;

    /// <summary>
    /// Apaga apenas ocorrência futura ainda não confirmada. O <c>previsto</c> é o que separa
    /// o que ainda é projeção do que já virou gasto do cliente — reprocessar a regra não pode
    /// reescrever o passado.
    /// </summary>
    public const string RemoverPrevistasAPartirDe = """
        DELETE FROM lancamento
        WHERE recorrencia_id = @recorrenciaId
          AND previsto = true
          AND data_caixa >= @aPartirDe
        """;
}
