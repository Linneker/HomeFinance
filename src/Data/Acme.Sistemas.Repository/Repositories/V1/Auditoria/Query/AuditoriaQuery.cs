namespace Acme.Sistemas.Repository.Repositories.V1.Auditoria.Query;

internal static class AuditoriaQuery
{
    /// <summary>Histórico de habilitação da conta, via função <c>SECURITY DEFINER</c> — o app não lê a tabela direto.</summary>
    public const string HistoricoExportacao =
        "SELECT id, evento, autor_id, detalhe, criado_em FROM historico_exportacao_da_conta(@conta)";
}
