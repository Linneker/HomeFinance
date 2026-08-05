using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations;

/// <summary>
/// Planos, assinaturas, transações e o registro das notificações dos provedores.
/// </summary>
/// <remarks>
/// O escopo de cada tabela é diferente, e as policies refletem isso:
/// <list type="bullet">
/// <item><c>plano</c> é catálogo do produto: todo mundo lê, ninguém escreve pela aplicação.</item>
/// <item>
/// <c>assinatura</c> e <c>transacao_pagamento</c> são do usuário, não do tenant — quem paga é a
/// pessoa, e ela leva a assinatura para qualquer espaço que abrir. Por isso o isolamento é por
/// <c>app_usuario_id()</c>, como em <c>consentimento</c>.
/// </item>
/// <item>
/// <c>notificacao_provedor</c> não tem dono: é o servidor da loja falando com o nosso, sem
/// ninguém autenticado. Fica sem RLS porque não há de quem isolá-la — nenhuma linha ali
/// pertence a um usuário — e o acesso é só do endpoint de webhook.
/// </item>
/// </list>
/// </remarks>
public sealed class V20260805011_AssinaturaEPagamento : MigrationBase
{
    public override long Versao => 20260805011;

    public override string Descricao => "Planos, assinaturas, transações e notificações de provedor";

    protected override string Sql => """
        CREATE TABLE plano (
            id                     uuid          PRIMARY KEY,
            codigo                 text          NOT NULL UNIQUE,
            nome                   text          NOT NULL,

            -- Zero é valor legítimo: é o plano que não concede compartilhamento nenhum.
            cota_compartilhamentos int           NOT NULL,
            preco_mensal           numeric(18,2) NOT NULL,
            ativo                  boolean       NOT NULL DEFAULT true,
            criado_em              timestamptz   NOT NULL,

            CONSTRAINT ck_plano_cota CHECK (cota_compartilhamentos >= 0),
            CONSTRAINT ck_plano_preco CHECK (preco_mensal >= 0)
        );

        CREATE TABLE assinatura (
            id                     uuid        PRIMARY KEY,
            usuario_id             uuid        NOT NULL REFERENCES usuario (id) ON DELETE CASCADE,
            plano_id               uuid        NOT NULL REFERENCES plano (id),

            -- 1 ativa, 2 carência, 3 suspensa, 4 cancelada.
            situacao               smallint    NOT NULL,

            -- 1 App Store, 2 Google Play, 3 Mercado Pago, 4 Asaas.
            canal                  smallint    NOT NULL,

            periodo_fim            timestamptz NOT NULL,
            carencia_ate           timestamptz NULL,

            -- Identificador devolvido pelo provedor. Nenhum dado de cartão entra aqui.
            comprovante_referencia text        NOT NULL,

            criado_em              timestamptz NOT NULL,
            atualizado_em          timestamptz NULL,

            CONSTRAINT ck_assinatura_situacao CHECK (situacao IN (1, 2, 3, 4)),
            CONSTRAINT ck_assinatura_canal CHECK (canal IN (1, 2, 3, 4))
        );

        -- Uma assinatura viva por conta. O índice parcial deixa o histórico de canceladas
        -- conviver com a vigente, que é o que permite recontratar depois de cancelar.
        CREATE UNIQUE INDEX ux_assinatura_vigente_por_usuario
            ON assinatura (usuario_id)
            WHERE situacao IN (1, 2, 3);

        -- O mesmo comprovante não ativa duas contas do aplicativo. É a regra que impede
        -- comprar uma vez e liberar a família inteira.
        CREATE UNIQUE INDEX ux_assinatura_comprovante
            ON assinatura (canal, comprovante_referencia);

        CREATE TABLE transacao_pagamento (
            id                     uuid          PRIMARY KEY,
            assinatura_id          uuid          NOT NULL REFERENCES assinatura (id) ON DELETE CASCADE,
            usuario_id             uuid          NOT NULL REFERENCES usuario (id) ON DELETE CASCADE,
            canal                  smallint      NOT NULL,
            identificador_provedor text          NOT NULL,
            valor                  numeric(18,2) NOT NULL,
            situacao               text          NOT NULL,
            criado_em              timestamptz   NOT NULL,

            CONSTRAINT ck_transacao_canal CHECK (canal IN (1, 2, 3, 4))
        );

        CREATE UNIQUE INDEX ux_transacao_provedor
            ON transacao_pagamento (canal, identificador_provedor);

        -- Sem tenant e sem usuário: a notificação chega antes de sabermos a quem pertence.
        -- O identificador do provedor é a chave de idempotência.
        CREATE TABLE notificacao_provedor (
            id            uuid        PRIMARY KEY,
            canal         smallint    NOT NULL,
            identificador text        NOT NULL,
            corpo         text        NOT NULL,
            recebida_em   timestamptz NOT NULL,

            CONSTRAINT ck_notificacao_canal CHECK (canal IN (1, 2, 3, 4))
        );

        CREATE UNIQUE INDEX ux_notificacao_provedor
            ON notificacao_provedor (canal, identificador);

        -- ------------------------------------------------------------------
        -- Isolamento
        -- ------------------------------------------------------------------
        ALTER TABLE plano ENABLE ROW LEVEL SECURITY;
        ALTER TABLE plano FORCE ROW LEVEL SECURITY;

        CREATE POLICY plano_sel ON plano FOR SELECT USING (true);

        ALTER TABLE assinatura ENABLE ROW LEVEL SECURITY;
        ALTER TABLE assinatura FORCE ROW LEVEL SECURITY;

        CREATE POLICY assinatura_sel ON assinatura FOR SELECT
            USING (usuario_id = app_usuario_id());
        CREATE POLICY assinatura_ins ON assinatura FOR INSERT
            WITH CHECK (usuario_id = app_usuario_id());
        CREATE POLICY assinatura_upd ON assinatura FOR UPDATE
            USING (usuario_id = app_usuario_id())
            WITH CHECK (usuario_id = app_usuario_id());

        ALTER TABLE transacao_pagamento ENABLE ROW LEVEL SECURITY;
        ALTER TABLE transacao_pagamento FORCE ROW LEVEL SECURITY;

        CREATE POLICY transacao_sel ON transacao_pagamento FOR SELECT
            USING (usuario_id = app_usuario_id());
        CREATE POLICY transacao_ins ON transacao_pagamento FOR INSERT
            WITH CHECK (usuario_id = app_usuario_id());

        GRANT SELECT, INSERT, UPDATE, DELETE
            ON plano, assinatura, transacao_pagamento, notificacao_provedor
            TO homefinance_app;

        -- Catálogo é configuração do produto: a aplicação lê, quem escreve é a migration.
        REVOKE INSERT, UPDATE, DELETE ON plano FROM homefinance_app;

        -- ------------------------------------------------------------------
        -- Aberturas controladas para o que chega sem usuário autenticado
        -- ------------------------------------------------------------------

        -- A notificação do provedor identifica a assinatura pela referência dele, não pela
        -- nossa conta. Sem esta função a policy devolveria vazio e toda renovação seria
        -- ignorada em silêncio.
        CREATE OR REPLACE FUNCTION assinatura_por_referencia(p_canal smallint, p_referencia text)
        RETURNS TABLE (
            id uuid,
            usuario_id uuid,
            plano_id uuid,
            situacao smallint,
            canal smallint,
            periodo_fim timestamptz,
            carencia_ate timestamptz,
            comprovante_referencia text,
            criado_em timestamptz,
            atualizado_em timestamptz)
        LANGUAGE sql
        SECURITY DEFINER
        STABLE
        SET search_path = public
        AS $$
            SELECT a.id, a.usuario_id, a.plano_id, a.situacao, a.canal, a.periodo_fim,
                   a.carencia_ate, a.comprovante_referencia, a.criado_em, a.atualizado_em
            FROM assinatura a
            WHERE a.canal = p_canal
              AND a.comprovante_referencia = p_referencia
        $$;

        -- Aplica o efeito da notificação. Devolve o número de linhas alteradas para que a
        -- aplicação saiba distinguir "atualizei" de "não existe assinatura para isto".
        CREATE OR REPLACE FUNCTION aplicar_situacao_assinatura(
            p_id uuid,
            p_situacao smallint,
            p_periodo_fim timestamptz,
            p_carencia_ate timestamptz,
            p_atualizado_em timestamptz)
        RETURNS integer
        LANGUAGE plpgsql
        SECURITY DEFINER
        SET search_path = public
        AS $$
        DECLARE
            afetadas integer;
        BEGIN
            UPDATE assinatura
            SET situacao      = p_situacao,
                periodo_fim   = p_periodo_fim,
                carencia_ate  = p_carencia_ate,
                atualizado_em = p_atualizado_em
            WHERE id = p_id;

            GET DIAGNOSTICS afetadas = ROW_COUNT;
            RETURN afetadas;
        END
        $$;

        -- A reconciliação roda sem requisição, como a rotina de alertas.
        CREATE OR REPLACE FUNCTION assinaturas_para_reconciliacao()
        RETURNS TABLE (
            id uuid,
            usuario_id uuid,
            plano_id uuid,
            situacao smallint,
            canal smallint,
            periodo_fim timestamptz,
            carencia_ate timestamptz,
            comprovante_referencia text,
            criado_em timestamptz,
            atualizado_em timestamptz)
        LANGUAGE sql
        SECURITY DEFINER
        STABLE
        SET search_path = public
        AS $$
            SELECT a.id, a.usuario_id, a.plano_id, a.situacao, a.canal, a.periodo_fim,
                   a.carencia_ate, a.comprovante_referencia, a.criado_em, a.atualizado_em
            FROM assinatura a
            JOIN usuario u ON u.id = a.usuario_id
            WHERE a.situacao IN (1, 2, 3)
              AND u.excluido_em IS NULL
            ORDER BY a.id
        $$;

        -- Abrir um tenant compartilhado exige que a assinatura do DONO esteja vigente — e o
        -- dono é outra pessoa, que a policy de assinatura esconde de propósito. A função devolve
        -- só a situação e as datas: nada de plano, valor ou canal.
        CREATE OR REPLACE FUNCTION situacao_assinatura_do_tenant(p_tenant_id uuid)
        RETURNS TABLE (situacao smallint, periodo_fim timestamptz, carencia_ate timestamptz)
        LANGUAGE sql
        SECURITY DEFINER
        STABLE
        SET search_path = public
        AS $$
            SELECT a.situacao, a.periodo_fim, a.carencia_ate
            FROM tenant t
            JOIN assinatura a ON a.usuario_id = t.usuario_id
            WHERE t.id = p_tenant_id
              AND a.situacao IN (1, 2, 3)
        $$;

        REVOKE EXECUTE ON FUNCTION situacao_assinatura_do_tenant(uuid) FROM PUBLIC;
        GRANT EXECUTE ON FUNCTION situacao_assinatura_do_tenant(uuid) TO homefinance_app;

        REVOKE EXECUTE ON FUNCTION assinatura_por_referencia(smallint, text) FROM PUBLIC;
        REVOKE EXECUTE ON FUNCTION aplicar_situacao_assinatura(uuid, smallint, timestamptz, timestamptz, timestamptz) FROM PUBLIC;
        REVOKE EXECUTE ON FUNCTION assinaturas_para_reconciliacao() FROM PUBLIC;

        GRANT EXECUTE ON FUNCTION assinatura_por_referencia(smallint, text) TO homefinance_app;
        GRANT EXECUTE ON FUNCTION aplicar_situacao_assinatura(uuid, smallint, timestamptz, timestamptz, timestamptz) TO homefinance_app;
        GRANT EXECUTE ON FUNCTION assinaturas_para_reconciliacao() TO homefinance_app;
        """;
}
