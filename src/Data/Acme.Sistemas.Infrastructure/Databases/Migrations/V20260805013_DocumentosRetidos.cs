using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations;

/// <summary>
/// Fila de documentos enviados durante a suspensão.
/// </summary>
/// <remarks>
/// O documento enviado com a assinatura suspensa é <em>guardado</em>, não recusado. Recusar o
/// envio faria a pessoa perder a fatura que só existia naquele e-mail, e o produto puniria por
/// inadimplência com perda de dado — que é diferente de suspender um serviço.
/// <para>
/// A tabela existe antes do produtor: o envio de documentos é da fase 2. Criá-la agora é o que
/// permite a política de retenção ser decidida e testada junto da política de assinatura, em vez
/// de aparecer como remendo quando o OCR chegar.
/// </para>
/// </remarks>
public sealed class V20260805013_DocumentosRetidos : MigrationBase
{
    public override long Versao => 20260805013;

    public override string Descricao => "Fila de documentos retidos durante a suspensão";

    protected override string Sql => """
        CREATE TABLE documento_retido (
            id             uuid        PRIMARY KEY,
            tenant_id      uuid        NOT NULL REFERENCES tenant (id) ON DELETE CASCADE,

            -- Onde o arquivo ficou. O conteúdo vive no MinIO; aqui só a referência.
            referencia     text        NOT NULL,
            tipo           text        NOT NULL,
            recebido_em    timestamptz NOT NULL,

            -- Nulo enquanto pendente. Preenchido quando a assinatura regulariza e a fila roda.
            processado_em  timestamptz NULL
        );

        CREATE INDEX ix_documento_retido_pendente
            ON documento_retido (tenant_id)
            WHERE processado_em IS NULL;

        ALTER TABLE documento_retido ENABLE ROW LEVEL SECURITY;
        ALTER TABLE documento_retido FORCE ROW LEVEL SECURITY;

        CREATE POLICY documento_retido_sel ON documento_retido FOR SELECT
            USING (tenant_id = app_tenant_id());
        CREATE POLICY documento_retido_ins ON documento_retido FOR INSERT
            WITH CHECK (tenant_id = app_tenant_id() AND NOT app_somente_leitura());
        CREATE POLICY documento_retido_upd ON documento_retido FOR UPDATE
            USING (tenant_id = app_tenant_id() AND NOT app_somente_leitura())
            WITH CHECK (tenant_id = app_tenant_id() AND NOT app_somente_leitura());

        GRANT SELECT, INSERT, UPDATE, DELETE ON documento_retido TO homefinance_app;
        """;
}
