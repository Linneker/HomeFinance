using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations;

/// <summary>
/// Exportação de dados: habilitação por conta, solicitações e trilha de auditoria.
/// </summary>
/// <remarks>
/// Três tabelas com escopos diferentes de propósito. <c>exportacao_habilitacao</c> é da conta e
/// não tem <c>tenant_id</c> — a habilitação existe antes de qualquer seleção de tenant e mantê-la
/// fora do escopo de tenant é o que garante que ligar a exportação não abra porta para o dado
/// financeiro. <c>exportacao_solicitacao</c> tem <c>tenant_id</c> e vive sob RLS como todo dado do
/// tenant. <c>auditoria</c> é append-only e não pertence a tenant nenhum.
/// <para>
/// A escrita da habilitação e a limpeza das solicitações expiradas rodam fora do contexto de um
/// tenant selecionado — a operação habilita sem tenant, a expiração varre todos. Seguem o mesmo
/// recurso da rotina de alertas: funções <c>SECURITY DEFINER</c> estreitas, que fazem só o
/// necessário e não abrem leitura de dado financeiro.
/// </para>
/// </remarks>
public sealed class V20260806014_Exportacao : MigrationBase
{
    public override long Versao => 20260806014;

    public override string Descricao => "Exportação de dados: habilitação, solicitações e auditoria";

    protected override string Sql => """
        -- ------------------------------------------------------------------
        -- Habilitação por conta. Uma linha por usuário, atualizada no lugar.
        -- ------------------------------------------------------------------
        CREATE TABLE exportacao_habilitacao (
            id             uuid        PRIMARY KEY,
            usuario_id     uuid        NOT NULL UNIQUE REFERENCES usuario (id) ON DELETE CASCADE,
            habilitada     boolean     NOT NULL,
            motivo         text        NOT NULL,
            autor_id       uuid        NOT NULL,
            criado_em      timestamptz NOT NULL,
            atualizado_em  timestamptz NULL
        );

        ALTER TABLE exportacao_habilitacao ENABLE ROW LEVEL SECURITY;
        ALTER TABLE exportacao_habilitacao FORCE ROW LEVEL SECURITY;

        -- O titular lê a própria habilitação — é o que a tela consulta para mostrar o recurso e
        -- o que a solicitação consulta para se liberar. Não há policy de escrita: o app não
        -- grava direto, só pela função SECURITY DEFINER abaixo.
        CREATE POLICY exportacao_habilitacao_sel ON exportacao_habilitacao FOR SELECT
            USING (usuario_id = app_usuario_id());

        GRANT SELECT ON exportacao_habilitacao TO homefinance_app;

        -- ------------------------------------------------------------------
        -- Solicitações. Dado do tenant, isolado como os demais.
        -- ------------------------------------------------------------------
        CREATE TABLE exportacao_solicitacao (
            id                      uuid        PRIMARY KEY,
            tenant_id               uuid        NOT NULL REFERENCES tenant (id) ON DELETE CASCADE,

            -- Quem pediu. O download é vinculado a esta conta, não ao tenant: um destinatário de
            -- compartilhamento enxerga o tenant, mas não pode baixar o que o dono gerou.
            usuario_solicitante_id  uuid        NOT NULL REFERENCES usuario (id) ON DELETE CASCADE,

            formato                 smallint    NOT NULL,
            situacao                smallint    NOT NULL,

            -- Nulos até a geração concluir; a referência volta a nulo quando o arquivo expira.
            referencia_arquivo      text        NULL,
            nome_arquivo            text        NULL,

            criado_em               timestamptz NOT NULL,
            concluido_em            timestamptz NULL,
            expira_em               timestamptz NULL,
            erro                    text        NULL
        );

        CREATE INDEX ix_exportacao_solicitacao_tenant
            ON exportacao_solicitacao (tenant_id, criado_em DESC);

        -- Apoia a varredura de expiração pela função SECURITY DEFINER.
        CREATE INDEX ix_exportacao_solicitacao_expira
            ON exportacao_solicitacao (expira_em)
            WHERE situacao = 3;

        ALTER TABLE exportacao_solicitacao ENABLE ROW LEVEL SECURITY;
        ALTER TABLE exportacao_solicitacao FORCE ROW LEVEL SECURITY;

        CREATE POLICY exportacao_solicitacao_sel ON exportacao_solicitacao FOR SELECT
            USING (tenant_id = app_tenant_id());
        CREATE POLICY exportacao_solicitacao_ins ON exportacao_solicitacao FOR INSERT
            WITH CHECK (tenant_id = app_tenant_id() AND NOT app_somente_leitura());
        CREATE POLICY exportacao_solicitacao_upd ON exportacao_solicitacao FOR UPDATE
            USING (tenant_id = app_tenant_id() AND NOT app_somente_leitura())
            WITH CHECK (tenant_id = app_tenant_id() AND NOT app_somente_leitura());

        GRANT SELECT, INSERT, UPDATE ON exportacao_solicitacao TO homefinance_app;

        -- ------------------------------------------------------------------
        -- Auditoria. Append-only, sem tenant, sem leitura pelo app do usuário
        -- final — a leitura é do back-office. Sem policy de SELECT, ninguém a
        -- lê por esta role; o INSERT é o único caminho aberto.
        -- ------------------------------------------------------------------
        CREATE TABLE auditoria (
            id                uuid        PRIMARY KEY,
            evento            smallint    NOT NULL,
            conta_usuario_id  uuid        NOT NULL,
            autor_id          uuid        NOT NULL,
            detalhe           text        NULL,
            criado_em         timestamptz NOT NULL
        );

        CREATE INDEX ix_auditoria_conta ON auditoria (conta_usuario_id, criado_em DESC);

        ALTER TABLE auditoria ENABLE ROW LEVEL SECURITY;
        ALTER TABLE auditoria FORCE ROW LEVEL SECURITY;

        CREATE POLICY auditoria_ins ON auditoria FOR INSERT WITH CHECK (true);

        GRANT INSERT ON auditoria TO homefinance_app;

        -- ------------------------------------------------------------------
        -- Funções SECURITY DEFINER para o trabalho fora do contexto de tenant.
        -- ------------------------------------------------------------------

        -- Grava a decisão de habilitação. Estreita e definer: o app não tem INSERT/UPDATE na
        -- tabela, só EXECUTE aqui, e a única coisa que a função faz é o upsert de uma linha.
        CREATE OR REPLACE FUNCTION definir_habilitacao_exportacao(
            p_id uuid,
            p_usuario uuid,
            p_habilitada boolean,
            p_motivo text,
            p_autor uuid,
            p_quando timestamptz)
        RETURNS void
        LANGUAGE sql
        SECURITY DEFINER
        SET search_path = public
        AS $$
            INSERT INTO exportacao_habilitacao (id, usuario_id, habilitada, motivo, autor_id, criado_em)
            VALUES (p_id, p_usuario, p_habilitada, p_motivo, p_autor, p_quando)
            ON CONFLICT (usuario_id) DO UPDATE
                SET habilitada = EXCLUDED.habilitada,
                    motivo = EXCLUDED.motivo,
                    autor_id = EXCLUDED.autor_id,
                    atualizado_em = p_quando;
        $$;

        REVOKE EXECUTE ON FUNCTION definir_habilitacao_exportacao(uuid, uuid, boolean, text, uuid, timestamptz) FROM PUBLIC;
        GRANT EXECUTE ON FUNCTION definir_habilitacao_exportacao(uuid, uuid, boolean, text, uuid, timestamptz) TO homefinance_app;

        -- Enumera solicitações concluídas cujo prazo passou, de todos os tenants. Só devolve o
        -- que a limpeza precisa para apagar o arquivo: nenhum dado financeiro.
        CREATE OR REPLACE FUNCTION exportacoes_expiradas(p_agora timestamptz)
        RETURNS TABLE (id uuid, tenant_id uuid, referencia_arquivo text)
        LANGUAGE sql
        SECURITY DEFINER
        STABLE
        SET search_path = public
        AS $$
            SELECT s.id, s.tenant_id, s.referencia_arquivo
            FROM exportacao_solicitacao s
            WHERE s.situacao = 3
              AND s.expira_em IS NOT NULL
              AND s.expira_em <= p_agora
            ORDER BY s.expira_em
        $$;

        REVOKE EXECUTE ON FUNCTION exportacoes_expiradas(timestamptz) FROM PUBLIC;
        GRANT EXECUTE ON FUNCTION exportacoes_expiradas(timestamptz) TO homefinance_app;

        -- Marca uma solicitação como expirada e esquece a referência do arquivo já removido.
        -- Idempotente: só age sobre a que ainda está concluída.
        CREATE OR REPLACE FUNCTION expirar_exportacao(p_id uuid)
        RETURNS void
        LANGUAGE sql
        SECURITY DEFINER
        SET search_path = public
        AS $$
            UPDATE exportacao_solicitacao
            SET situacao = 4, referencia_arquivo = NULL
            WHERE id = p_id AND situacao = 3;
        $$;

        REVOKE EXECUTE ON FUNCTION expirar_exportacao(uuid) FROM PUBLIC;
        GRANT EXECUTE ON FUNCTION expirar_exportacao(uuid) TO homefinance_app;

        -- Cancela as solicitações ainda não concluídas de uma conta, na desabilitação. Parte do
        -- administrador, que não tem o tenant do titular selecionado.
        CREATE OR REPLACE FUNCTION cancelar_exportacoes_pendentes(p_usuario uuid)
        RETURNS integer
        LANGUAGE sql
        SECURITY DEFINER
        SET search_path = public
        AS $$
            WITH canceladas AS (
                UPDATE exportacao_solicitacao
                SET situacao = 5
                WHERE usuario_solicitante_id = p_usuario AND situacao IN (1, 2)
                RETURNING 1
            )
            SELECT count(*)::int FROM canceladas;
        $$;

        REVOKE EXECUTE ON FUNCTION cancelar_exportacoes_pendentes(uuid) FROM PUBLIC;
        GRANT EXECUTE ON FUNCTION cancelar_exportacoes_pendentes(uuid) TO homefinance_app;
        """;
}
