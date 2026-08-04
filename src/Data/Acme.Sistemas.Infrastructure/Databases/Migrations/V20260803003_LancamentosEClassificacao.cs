using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations;

/// <summary>Categorias, lançamentos, participação e regras de classificação.</summary>
public sealed class V20260803003_LancamentosEClassificacao : MigrationBase
{
    public override long Versao => 20260803003;

    public override string Descricao => "Categorias, lançamentos, participação e regras";

    protected override string Sql => """
        CREATE TABLE categoria (
            id        uuid PRIMARY KEY,
            tenant_id uuid NOT NULL REFERENCES tenant (id) ON DELETE CASCADE,
            nome      text NOT NULL,
            criado_em timestamptz NOT NULL
        );

        CREATE UNIQUE INDEX ux_categoria_tenant_nome ON categoria (tenant_id, lower(nome));

        CREATE TABLE lancamento (
            id                    uuid          PRIMARY KEY,
            tenant_id             uuid          NOT NULL REFERENCES tenant (id) ON DELETE CASCADE,
            descricao             text          NOT NULL,
            valor                 numeric(18,2) NOT NULL,

            -- As duas datas são obrigatórias, sempre. Competência é a visão primária;
            -- caixa é a secundária. Guardar só uma inviabilizaria a outra sem reprocessar
            -- o histórico inteiro dos clientes.
            data_competencia      date          NOT NULL,
            data_caixa            date          NOT NULL,

            categoria_id          uuid          NULL REFERENCES categoria (id) ON DELETE SET NULL,

            -- 0 = pendente, 1 = aplicada por regra, 2 = atribuída manualmente.
            -- É o que permite a regra nova não sobrescrever classificação manual.
            origem_classificacao  smallint      NOT NULL DEFAULT 0,

            -- Parcelas são lançamentos irmãos: mesma competência, caixas distintas.
            grupo_parcelamento_id uuid          NULL,
            parcela_numero        int           NULL,
            parcela_total         int           NULL,

            recorrencia_id        uuid          NULL,
            previsto              boolean       NOT NULL DEFAULT false,

            criado_em             timestamptz   NOT NULL,
            alterado_em           timestamptz   NULL,

            CONSTRAINT ck_lancamento_valor_positivo CHECK (valor > 0),
            CONSTRAINT ck_lancamento_origem CHECK (origem_classificacao IN (0, 1, 2)),
            CONSTRAINT ck_lancamento_parcela CHECK (
                (parcela_numero IS NULL AND parcela_total IS NULL)
                OR (parcela_numero BETWEEN 1 AND parcela_total)
            )
        );

        -- Atribuição do gasto a pessoas. Nesta fase é sempre uma linha a 100%; quando o tenant
        -- compartilhado com rateio chegar, vira N linhas somando 100 e nenhuma consulta de
        -- relatório muda.
        CREATE TABLE lancamento_participacao (
            id            uuid          PRIMARY KEY,
            tenant_id     uuid          NOT NULL REFERENCES tenant (id) ON DELETE CASCADE,
            lancamento_id uuid          NOT NULL REFERENCES lancamento (id) ON DELETE CASCADE,
            usuario_id    uuid          NOT NULL REFERENCES usuario (id) ON DELETE CASCADE,
            percentual    numeric(5,2)  NOT NULL,

            CONSTRAINT ck_participacao_percentual CHECK (percentual > 0 AND percentual <= 100)
        );

        CREATE UNIQUE INDEX ux_participacao_lancamento_usuario
            ON lancamento_participacao (lancamento_id, usuario_id);

        CREATE TABLE regra_classificacao (
            id           uuid        PRIMARY KEY,
            tenant_id    uuid        NOT NULL REFERENCES tenant (id) ON DELETE CASCADE,

            -- Já normalizado (maiúsculas, sem acento) na gravação: normalizar na consulta
            -- impediria o uso de índice.
            padrao       text        NOT NULL,
            categoria_id uuid        NOT NULL REFERENCES categoria (id) ON DELETE CASCADE,
            criada_em    timestamptz NOT NULL
        );

        CREATE UNIQUE INDEX ux_regra_tenant_padrao ON regra_classificacao (tenant_id, padrao);
        """;
}
