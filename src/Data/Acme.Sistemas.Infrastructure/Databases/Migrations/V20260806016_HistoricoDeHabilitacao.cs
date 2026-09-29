using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations;

/// <summary>
/// Leitura da trilha de habilitação da exportação para o back-office.
/// </summary>
/// <remarks>
/// A tabela <c>auditoria</c> é append-only e não tem policy de SELECT: o role da aplicação só
/// insere. O histórico que a tela administrativa mostra vem por esta função <c>SECURITY
/// DEFINER</c>, que devolve apenas os eventos de habilitação e desabilitação de uma conta —
/// não a trilha inteira, e sem tocar em nada além do que a operação precisa ver.
/// </remarks>
public sealed class V20260806016_HistoricoDeHabilitacao : MigrationBase
{
    public override long Versao => 20260806016;

    public override string Descricao => "Leitura da trilha de habilitação da exportação para o back-office";

    protected override string Sql => """
        -- Eventos 1 (habilitada) e 2 (desabilitada) da conta, do mais recente ao mais antigo.
        CREATE OR REPLACE FUNCTION historico_exportacao_da_conta(p_conta uuid)
        RETURNS TABLE (
            id        uuid,
            evento    smallint,
            autor_id  uuid,
            detalhe   text,
            criado_em timestamptz)
        LANGUAGE sql
        SECURITY DEFINER
        STABLE
        SET search_path = public
        AS $$
            SELECT a.id, a.evento, a.autor_id, a.detalhe, a.criado_em
            FROM auditoria a
            WHERE a.conta_usuario_id = p_conta
              AND a.evento IN (1, 2)
            ORDER BY a.criado_em DESC
        $$;

        REVOKE EXECUTE ON FUNCTION historico_exportacao_da_conta(uuid) FROM PUBLIC;
        GRANT EXECUTE ON FUNCTION historico_exportacao_da_conta(uuid) TO homefinance_app;
        """;
}
