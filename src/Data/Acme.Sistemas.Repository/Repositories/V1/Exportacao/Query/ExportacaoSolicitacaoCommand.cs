namespace Acme.Sistemas.Repository.Repositories.V1.Exportacao.Query;

internal static class ExportacaoSolicitacaoCommand
{
    public const string Criar = """
        INSERT INTO exportacao_solicitacao
            (id, tenant_id, usuario_solicitante_id, formato, situacao, criado_em)
        VALUES (@id, @tenantId, @usuarioSolicitanteId, @formato, @situacao, @criadoEm)
        """;

    /// <summary>
    /// Atualiza o que o trabalhador da fila muda ao longo da geração — situação, referência,
    /// nome, conclusão, expiração e erro. Chave por id, sob RLS do tenant.
    /// </summary>
    public const string Atualizar = """
        UPDATE exportacao_solicitacao
        SET situacao = @situacao,
            referencia_arquivo = @referenciaArquivo,
            nome_arquivo = @nomeArquivo,
            concluido_em = @concluidoEm,
            expira_em = @expiraEm,
            erro = @erro
        WHERE id = @id
        """;

    /// <summary>Expira uma solicitação e esquece a referência do arquivo removido — via definer.</summary>
    public const string Expirar = "SELECT expirar_exportacao(@id)";

    /// <summary>Cancela as pendentes de uma conta na desabilitação — via definer, sem tenant.</summary>
    public const string CancelarPendentes = "SELECT cancelar_exportacoes_pendentes(@usuarioId)";
}
