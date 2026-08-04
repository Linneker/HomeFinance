using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations;

/// <summary>Usuário, consentimento, tenant e compartilhamento.</summary>
public sealed class V20260803002_ContasEAcesso : MigrationBase
{
    public override long Versao => 20260803002;

    public override string Descricao => "Tabelas de conta, tenant e compartilhamento";

    protected override string Sql => """
        CREATE TABLE usuario (
            id           uuid        PRIMARY KEY,
            email        text        NOT NULL,
            senha_hash   text        NOT NULL,
            criado_em    timestamptz NOT NULL,
            excluido_em  timestamptz NULL
        );

        -- Unicidade sem diferenciar caixa: "Ana@x.com" e "ana@x.com" são a mesma conta.
        CREATE UNIQUE INDEX ux_usuario_email ON usuario (lower(email)) WHERE excluido_em IS NULL;

        CREATE TABLE consentimento (
            id         uuid        PRIMARY KEY,
            usuario_id uuid        NOT NULL REFERENCES usuario (id) ON DELETE CASCADE,
            documento  text        NOT NULL,
            versao     text        NOT NULL,
            aceito_em  timestamptz NOT NULL
        );

        CREATE INDEX ix_consentimento_usuario ON consentimento (usuario_id, documento);

        -- 1:1 com usuário: o tenant nasce junto com a conta.
        CREATE TABLE tenant (
            id         uuid        PRIMARY KEY,
            usuario_id uuid        NOT NULL UNIQUE REFERENCES usuario (id) ON DELETE CASCADE,
            criado_em  timestamptz NOT NULL
        );

        CREATE TABLE tenant_compartilhamento (
            id                      uuid        PRIMARY KEY,
            tenant_id               uuid        NOT NULL REFERENCES tenant (id) ON DELETE CASCADE,
            usuario_destinatario_id uuid        NOT NULL REFERENCES usuario (id) ON DELETE CASCADE,
            inclui_scr              boolean     NOT NULL DEFAULT false,
            concedido_em            timestamptz NOT NULL,
            revogado_em             timestamptz NULL,

            -- Compartilhar consigo mesmo não significa nada e confundiria a resolução de acesso.
            CONSTRAINT ck_compartilhamento_destinatario_distinto
                CHECK (usuario_destinatario_id IS NOT NULL)
        );

        -- Só um compartilhamento vigente por par; revogados ficam no histórico.
        CREATE UNIQUE INDEX ux_compartilhamento_vigente
            ON tenant_compartilhamento (tenant_id, usuario_destinatario_id)
            WHERE revogado_em IS NULL;

        CREATE INDEX ix_compartilhamento_destinatario
            ON tenant_compartilhamento (usuario_destinatario_id)
            WHERE revogado_em IS NULL;
        """;
}
