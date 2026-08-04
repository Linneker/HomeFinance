using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations;

/// <summary>
/// Tabela da regra de recorrência.
/// </summary>
/// <remarks>
/// O schema do grupo 2 criou <c>lancamento.recorrencia_id</c> sem a tabela correspondente. Sem
/// ela, "encerrar a recorrência" não teria onde deixar marca e uma nova materialização
/// recriaria as ocorrências que o usuário acabou de encerrar.
/// <para>
/// A chave estrangeira é <c>ON DELETE SET NULL</c>, e não <c>CASCADE</c>: apagar a regra não
/// pode apagar gasto já realizado. Encerrar é <c>UPDATE</c>, então o caminho normal nunca
/// chega aqui — isto vale para o dia em que alguém apagar a linha por outro caminho.
/// </para>
/// </remarks>
public sealed class V20260804009_Recorrencia : MigrationBase
{
    public override long Versao => 20260804009;

    public override string Descricao => "Tabela de recorrência, RLS e privilégios";

    protected override string Sql => """
        CREATE TABLE recorrencia (
            id           uuid          PRIMARY KEY,
            tenant_id    uuid          NOT NULL REFERENCES tenant (id) ON DELETE CASCADE,
            descricao    text          NOT NULL,
            valor        numeric(18,2) NOT NULL,

            -- Dia do mês da ocorrência. Em mês mais curto a materialização recua para o
            -- último dia: dia 31 em fevereiro não pode simplesmente deixar de existir.
            dia_do_mes   smallint      NOT NULL,

            categoria_id uuid          NULL REFERENCES categoria (id) ON DELETE SET NULL,
            inicio       date          NOT NULL,

            -- Marca do encerramento. É o que torna a rematerialização idempotente.
            encerrada_em timestamptz   NULL,
            criado_em    timestamptz   NOT NULL,

            CONSTRAINT ck_recorrencia_valor CHECK (valor > 0),
            CONSTRAINT ck_recorrencia_dia CHECK (dia_do_mes BETWEEN 1 AND 31)
        );

        CREATE INDEX ix_recorrencia_tenant_ativa
            ON recorrencia (tenant_id)
            WHERE encerrada_em IS NULL;

        ALTER TABLE lancamento
            ADD CONSTRAINT fk_lancamento_recorrencia
            FOREIGN KEY (recorrencia_id) REFERENCES recorrencia (id) ON DELETE SET NULL;

        ALTER TABLE recorrencia ENABLE ROW LEVEL SECURITY;
        ALTER TABLE recorrencia FORCE ROW LEVEL SECURITY;

        CREATE POLICY recorrencia_sel ON recorrencia FOR SELECT
            USING (tenant_id = app_tenant_id());
        CREATE POLICY recorrencia_ins ON recorrencia FOR INSERT
            WITH CHECK (tenant_id = app_tenant_id() AND NOT app_somente_leitura());
        CREATE POLICY recorrencia_upd ON recorrencia FOR UPDATE
            USING (tenant_id = app_tenant_id() AND NOT app_somente_leitura())
            WITH CHECK (tenant_id = app_tenant_id() AND NOT app_somente_leitura());
        CREATE POLICY recorrencia_del ON recorrencia FOR DELETE
            USING (tenant_id = app_tenant_id() AND NOT app_somente_leitura());

        -- Tabela criada depois da migration de privilégios, então repete o GRANT.
        GRANT SELECT, INSERT, UPDATE, DELETE ON recorrencia TO homefinance_app;
        """;
}
