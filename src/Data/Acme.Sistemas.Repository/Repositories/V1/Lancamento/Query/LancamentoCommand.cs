namespace Acme.Sistemas.Repository.Repositories.V1.Lancamento.Query;

internal static class LancamentoCommand
{
    public const string Inserir = """
        INSERT INTO lancamento (
            id, tenant_id, descricao, valor, data_competencia, data_caixa, categoria_id,
            origem_classificacao, grupo_parcelamento_id, parcela_numero, parcela_total,
            recorrencia_id, previsto, criado_em)
        VALUES (
            @id, @tenantId, @descricao, @valor, @dataCompetencia, @dataCaixa, @categoriaId,
            @origemClassificacao, @grupoParcelamentoId, @parcelaNumero, @parcelaTotal,
            @recorrenciaId, @previsto, @criadoEm)
        """;

    public const string InserirParticipacao = """
        INSERT INTO lancamento_participacao (id, tenant_id, lancamento_id, usuario_id, percentual)
        VALUES (@id, @tenantId, @lancamentoId, @usuarioId, @percentual)
        """;

    /// <summary>
    /// Não altera as colunas de parcelamento nem de recorrência: mudá-las por aqui desfaria
    /// o vínculo entre as linhas irmãs e deixaria "3/10" apontando para um grupo incompleto.
    /// </summary>
    public const string Alterar = """
        UPDATE lancamento
        SET descricao        = @descricao,
            valor            = @valor,
            data_competencia = @dataCompetencia,
            data_caixa       = @dataCaixa,
            categoria_id     = @categoriaId,
            origem_classificacao = @origemClassificacao,
            alterado_em      = @alteradoEm
        WHERE id = @id
        """;

    public const string Remover = """
        DELETE FROM lancamento WHERE id = @id
        """;
}
