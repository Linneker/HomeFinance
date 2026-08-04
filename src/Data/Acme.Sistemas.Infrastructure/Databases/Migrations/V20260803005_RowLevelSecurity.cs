using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations;

/// <summary>
/// Isolamento entre tenants imposto pelo banco.
/// </summary>
/// <remarks>
/// Com SQL puro não existe filtro global de query: um <c>WHERE</c> esquecido vazaria dados
/// financeiros entre famílias. RLS é o que transforma esse esquecimento em resultado vazio.
/// <para>
/// <c>FORCE</c> é obrigatório junto com <c>ENABLE</c>: sem ele, o dono da tabela ignora as
/// policies, e todo o isolamento vira decoração.
/// </para>
/// </remarks>
public sealed class V20260803005_RowLevelSecurity : MigrationBase
{
    public override long Versao => 20260803005;

    public override string Descricao => "Row Level Security com FORCE e policies por operação";

    protected override string Sql => """
        -- ------------------------------------------------------------------
        -- Tabelas de dado financeiro: isoladas pelo tenant selecionado.
        -- ------------------------------------------------------------------
        DO $$
        DECLARE
            tabela text;
            tabelas text[] := ARRAY[
                'categoria',
                'lancamento',
                'lancamento_participacao',
                'regra_classificacao',
                'fonte_renda',
                'orcamento_mensal',
                'alerta',
                'onboarding_estado'
            ];
        BEGIN
            FOREACH tabela IN ARRAY tabelas LOOP
                EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', tabela);
                EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', tabela);

                -- Leitura: vale também para quem recebeu o tenant por compartilhamento.
                -- Quem valida se o usuário pode selecionar aquele tenant é a aplicação,
                -- uma vez, antes de definir o parâmetro de sessão.
                EXECUTE format(
                    'CREATE POLICY %I ON %I FOR SELECT USING (tenant_id = app_tenant_id())',
                    tabela || '_sel', tabela);

                -- Escrita: exige o mesmo tenant E que a requisição não seja somente leitura.
                -- É assim que o compartilhamento read-only é imposto pelo banco, e não por
                -- verificação espalhada nos handlers.
                EXECUTE format(
                    'CREATE POLICY %I ON %I FOR INSERT WITH CHECK (tenant_id = app_tenant_id() AND NOT app_somente_leitura())',
                    tabela || '_ins', tabela);

                EXECUTE format(
                    'CREATE POLICY %I ON %I FOR UPDATE USING (tenant_id = app_tenant_id() AND NOT app_somente_leitura()) WITH CHECK (tenant_id = app_tenant_id() AND NOT app_somente_leitura())',
                    tabela || '_upd', tabela);

                EXECUTE format(
                    'CREATE POLICY %I ON %I FOR DELETE USING (tenant_id = app_tenant_id() AND NOT app_somente_leitura())',
                    tabela || '_del', tabela);
            END LOOP;
        END
        $$;

        -- ------------------------------------------------------------------
        -- Tabelas de conta: lidas ANTES de existir tenant selecionado, então são
        -- isoladas pelo usuário autenticado. Sem isto, um WHERE esquecido aqui
        -- exporia e-mails e com quem cada pessoa compartilha.
        -- ------------------------------------------------------------------
        ALTER TABLE usuario ENABLE ROW LEVEL SECURITY;
        ALTER TABLE usuario FORCE ROW LEVEL SECURITY;

        CREATE POLICY usuario_sel ON usuario FOR SELECT
            USING (id = app_usuario_id());
        CREATE POLICY usuario_upd ON usuario FOR UPDATE
            USING (id = app_usuario_id()) WITH CHECK (id = app_usuario_id());
        CREATE POLICY usuario_del ON usuario FOR DELETE
            USING (id = app_usuario_id());

        -- INSERT sem restrição de contexto: a criação de conta acontece antes de haver
        -- usuário autenticado. É a única escrita nessa condição em todo o schema.
        CREATE POLICY usuario_ins ON usuario FOR INSERT WITH CHECK (true);

        ALTER TABLE consentimento ENABLE ROW LEVEL SECURITY;
        ALTER TABLE consentimento FORCE ROW LEVEL SECURITY;

        CREATE POLICY consentimento_sel ON consentimento FOR SELECT
            USING (usuario_id = app_usuario_id());
        CREATE POLICY consentimento_ins ON consentimento FOR INSERT
            WITH CHECK (usuario_id IS NOT NULL);

        ALTER TABLE tenant ENABLE ROW LEVEL SECURITY;
        ALTER TABLE tenant FORCE ROW LEVEL SECURITY;

        -- O OR com app_tenant_id() evita subconsulta em tenant_compartilhamento, que criaria
        -- recursão entre as policies das duas tabelas.
        CREATE POLICY tenant_sel ON tenant FOR SELECT
            USING (usuario_id = app_usuario_id() OR id = app_tenant_id());
        CREATE POLICY tenant_ins ON tenant FOR INSERT
            WITH CHECK (usuario_id IS NOT NULL);
        CREATE POLICY tenant_del ON tenant FOR DELETE
            USING (usuario_id = app_usuario_id());

        ALTER TABLE tenant_compartilhamento ENABLE ROW LEVEL SECURITY;
        ALTER TABLE tenant_compartilhamento FORCE ROW LEVEL SECURITY;

        -- Dono vê o que concedeu; destinatário vê o que recebeu.
        CREATE POLICY compartilhamento_sel ON tenant_compartilhamento FOR SELECT
            USING (
                usuario_destinatario_id = app_usuario_id()
                OR tenant_id IN (SELECT t.id FROM tenant t WHERE t.usuario_id = app_usuario_id())
            );

        -- Conceder e revogar são exclusivos do dono: é o que impede o destinatário de
        -- repassar o acesso a um terceiro.
        CREATE POLICY compartilhamento_ins ON tenant_compartilhamento FOR INSERT
            WITH CHECK (tenant_id IN (SELECT t.id FROM tenant t WHERE t.usuario_id = app_usuario_id()));
        CREATE POLICY compartilhamento_upd ON tenant_compartilhamento FOR UPDATE
            USING (tenant_id IN (SELECT t.id FROM tenant t WHERE t.usuario_id = app_usuario_id()))
            WITH CHECK (tenant_id IN (SELECT t.id FROM tenant t WHERE t.usuario_id = app_usuario_id()));

        -- Catálogo do onboarding é configuração do produto, não dado de cliente: leitura
        -- liberada, escrita apenas por migration.
        ALTER TABLE onboarding_requisito ENABLE ROW LEVEL SECURITY;
        ALTER TABLE onboarding_requisito FORCE ROW LEVEL SECURITY;

        CREATE POLICY requisito_sel ON onboarding_requisito FOR SELECT USING (true);
        """;
}
