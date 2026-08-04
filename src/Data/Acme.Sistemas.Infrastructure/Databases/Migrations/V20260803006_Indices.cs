using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations;

/// <summary>
/// Índices de apoio às consultas por tenant, competência e caixa.
/// </summary>
/// <remarks>
/// Todo índice começa por <c>tenant_id</c> porque a policy de RLS acrescenta esse predicado a
/// toda consulta: um índice que não o tenha na frente não é usado.
/// </remarks>
public sealed class V20260803006_Indices : MigrationBase
{
    public override long Versao => 20260803006;

    public override string Descricao => "Índices por tenant, competência e caixa";

    protected override string Sql => """
        -- Extrato e resumo do mês em competência: a visão primária.
        CREATE INDEX ix_lancamento_tenant_competencia
            ON lancamento (tenant_id, data_competencia);

        -- Projeção de saldo e alerta de caixa percorrem por data de caixa.
        CREATE INDEX ix_lancamento_tenant_caixa
            ON lancamento (tenant_id, data_caixa);

        -- Fila de classificação pendente.
        CREATE INDEX ix_lancamento_pendente_classificacao
            ON lancamento (tenant_id, data_competencia)
            WHERE categoria_id IS NULL;

        -- Resumo por categoria dentro do mês.
        CREATE INDEX ix_lancamento_tenant_categoria_competencia
            ON lancamento (tenant_id, categoria_id, data_competencia);

        -- Exibição do agrupamento "3/10" das parcelas.
        CREATE INDEX ix_lancamento_grupo_parcelamento
            ON lancamento (tenant_id, grupo_parcelamento_id)
            WHERE grupo_parcelamento_id IS NOT NULL;

        -- Regeneração das ocorrências previstas ainda não confirmadas.
        CREATE INDEX ix_lancamento_recorrencia
            ON lancamento (tenant_id, recorrencia_id)
            WHERE recorrencia_id IS NOT NULL;

        -- Apuração de valor por pessoa: os relatórios agregam por esta tabela, não por tenant_id.
        CREATE INDEX ix_participacao_tenant_usuario
            ON lancamento_participacao (tenant_id, usuario_id);

        CREATE INDEX ix_alerta_tenant_estado
            ON alerta (tenant_id, estado, tipo);
        """;
}
