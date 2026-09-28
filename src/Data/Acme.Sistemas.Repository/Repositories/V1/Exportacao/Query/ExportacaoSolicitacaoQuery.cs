namespace Acme.Sistemas.Repository.Repositories.V1.Exportacao.Query;

internal static class ExportacaoSolicitacaoQuery
{
    /// <summary>Uma solicitação do tenant ativo. A RLS já restringe ao tenant; o id é a chave.</summary>
    public const string PorId = """
        SELECT id, tenant_id, usuario_solicitante_id, formato, situacao,
               referencia_arquivo, nome_arquivo, criado_em, concluido_em, expira_em, erro
        FROM exportacao_solicitacao
        WHERE id = @id
        """;

    /// <summary>Concluídas com prazo vencido, de todos os tenants — pela função SECURITY DEFINER.</summary>
    public const string Expiradas = "SELECT id, tenant_id, referencia_arquivo FROM exportacoes_expiradas(@agora)";
}
