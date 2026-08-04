using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations;

/// <summary>Renda, orçamento, alertas e onboarding.</summary>
public sealed class V20260803004_OrcamentoAlertasOnboarding : MigrationBase
{
    public override long Versao => 20260803004;

    public override string Descricao => "Renda, orçamento mensal, alertas e onboarding";

    protected override string Sql => """
        CREATE TABLE fonte_renda (
            id              uuid          PRIMARY KEY,
            tenant_id       uuid          NOT NULL REFERENCES tenant (id) ON DELETE CASCADE,
            nome            text          NOT NULL,
            valor           numeric(18,2) NOT NULL,
            dia_recebimento smallint      NOT NULL,

            -- Vigência por período: alterar a renda preserva os meses já fechados.
            vigencia_inicio date          NOT NULL,
            vigencia_fim    date          NULL,
            criado_em       timestamptz   NOT NULL,

            CONSTRAINT ck_renda_valor CHECK (valor > 0),
            CONSTRAINT ck_renda_dia CHECK (dia_recebimento BETWEEN 1 AND 31),
            CONSTRAINT ck_renda_vigencia CHECK (vigencia_fim IS NULL OR vigencia_fim >= vigencia_inicio)
        );

        CREATE INDEX ix_renda_tenant_vigencia ON fonte_renda (tenant_id, vigencia_inicio, vigencia_fim);

        CREATE TABLE orcamento_mensal (
            id              uuid          PRIMARY KEY,
            tenant_id       uuid          NOT NULL REFERENCES tenant (id) ON DELETE CASCADE,
            categoria_id    uuid          NOT NULL REFERENCES categoria (id) ON DELETE CASCADE,

            -- Sempre o primeiro dia do mês: guardar competência como data evita a comparação
            -- de string 'AAAA-MM' que quebra a ordenação na virada de ano.
            competencia     date          NOT NULL,
            valor_planejado numeric(18,2) NOT NULL,
            criado_em       timestamptz   NOT NULL,

            CONSTRAINT ck_orcamento_valor CHECK (valor_planejado >= 0),
            CONSTRAINT ck_orcamento_primeiro_dia CHECK (date_trunc('month', competencia) = competencia)
        );

        CREATE UNIQUE INDEX ux_orcamento_tenant_categoria_competencia
            ON orcamento_mensal (tenant_id, categoria_id, competencia);

        CREATE TABLE alerta (
            id             uuid        PRIMARY KEY,
            tenant_id      uuid        NOT NULL REFERENCES tenant (id) ON DELETE CASCADE,

            -- 1 = competência (estouro de orçamento), 2 = caixa (saldo projetado insuficiente).
            -- Tipos distintos de propósito: não se unificam.
            tipo           smallint    NOT NULL,

            -- Identifica a condição, não a ocorrência. É o que impede o mesmo alerta de ser
            -- emitido de novo enquanto a condição não mudar.
            chave_condicao text        NOT NULL,

            -- 1 = aberto, 2 = encerrado, 3 = dispensado.
            estado         smallint    NOT NULL,
            mensagem       text        NOT NULL,
            criado_em      timestamptz NOT NULL,
            encerrado_em   timestamptz NULL,
            dispensado_em  timestamptz NULL,

            CONSTRAINT ck_alerta_tipo CHECK (tipo IN (1, 2)),
            CONSTRAINT ck_alerta_estado CHECK (estado IN (1, 2, 3))
        );

        CREATE UNIQUE INDEX ux_alerta_condicao_aberta
            ON alerta (tenant_id, chave_condicao)
            WHERE estado = 1;

        -- Catálogo global, sem tenant: é configuração do produto, não dado de cliente.
        CREATE TABLE onboarding_requisito (
            codigo     text     PRIMARY KEY,
            descricao  text     NOT NULL,
            ordem      int      NOT NULL,

            -- Declarado x ativo: requisitos que dependem de recurso ainda não entregue
            -- aparecem na barra de progresso sem bloquear nada.
            ativo      boolean  NOT NULL DEFAULT false,
            bloqueante boolean  NOT NULL DEFAULT false
        );

        CREATE TABLE onboarding_estado (
            id                uuid        PRIMARY KEY,
            tenant_id         uuid        NOT NULL REFERENCES tenant (id) ON DELETE CASCADE,
            requisito_codigo  text        NOT NULL REFERENCES onboarding_requisito (codigo) ON DELETE CASCADE,
            cumprido_em       timestamptz NULL
        );

        CREATE UNIQUE INDEX ux_onboarding_estado_tenant_requisito
            ON onboarding_estado (tenant_id, requisito_codigo);

        INSERT INTO onboarding_requisito (codigo, descricao, ordem, ativo, bloqueante) VALUES
            ('renda_declarada',      'Informe sua renda mensal',                        1, true,  true),
            ('registrato_3_meses',   'Envie o Registrato dos últimos 3 meses',          2, false, false),
            ('faturas_3_meses',      'Envie as faturas de cartão dos últimos 3 meses',  3, false, false),
            ('contas_basicas',       'Envie os boletos de contas básicas',              4, false, false);
        """;
}
