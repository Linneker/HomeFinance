using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations;

/// <summary>
/// Role da aplicação e as funções que leem o contexto da requisição.
/// </summary>
/// <remarks>
/// As funções existem para que as policies não repitam <c>current_setting</c> com tratamento de
/// nulo e string vazia em dezenas de lugares — e para que, se a leitura do contexto precisar
/// mudar, mude num ponto só.
/// </remarks>
public sealed class V20260803001_RoleEFuncoesDeContexto : MigrationBase
{
    public override long Versao => 20260803001;

    public override string Descricao => "Role da aplicação e funções de contexto de requisição";

    protected override string Sql => """
        CREATE EXTENSION IF NOT EXISTS vector;

        -- Role de conexão da aplicação.
        --
        -- A aplicação NUNCA conecta como dono do banco. O usuário criado pelo POSTGRES_USER é
        -- superusuário, e superusuário ignora RLS mesmo com FORCE — o isolamento inteiro fica
        -- inerte sem nenhum sinal de erro. Este role existe para que as policies tenham efeito.
        --
        -- LOGIN sim, senha não: a senha é definida fora da migration, a partir do OpenBao, para
        -- não versionar credencial. Sem senha definida o role não autentica.
        DO $$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'homefinance_app') THEN
                CREATE ROLE homefinance_app LOGIN NOBYPASSRLS NOSUPERUSER NOCREATEDB NOCREATEROLE;
            ELSE
                ALTER ROLE homefinance_app LOGIN NOBYPASSRLS NOSUPERUSER;
            END IF;
        END
        $$;

        GRANT USAGE ON SCHEMA public TO homefinance_app;

        ALTER DEFAULT PRIVILEGES IN SCHEMA public
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO homefinance_app;

        -- Tenant selecionado na requisição. NULL quando não há contexto (ex.: administrador),
        -- e NULL em comparação nunca casa — a consulta volta vazia por construção.
        CREATE OR REPLACE FUNCTION app_tenant_id() RETURNS uuid
            LANGUAGE sql STABLE
            AS $$ SELECT nullif(current_setting('app.tenant_id', true), '')::uuid $$;

        -- Usuário autenticado. Protege as tabelas de conta, que são lidas ANTES de existir
        -- tenant selecionado e por isso não podem depender de app_tenant_id().
        CREATE OR REPLACE FUNCTION app_usuario_id() RETURNS uuid
            LANGUAGE sql STABLE
            AS $$ SELECT nullif(current_setting('app.usuario_id', true), '')::uuid $$;

        -- Default TRUE de propósito: sem contexto explícito, a requisição é somente leitura.
        -- Um esquecimento vira recusa de escrita, não escrita indevida.
        CREATE OR REPLACE FUNCTION app_somente_leitura() RETURNS boolean
            LANGUAGE sql STABLE
            AS $$ SELECT coalesce(nullif(current_setting('app.readonly', true), '')::boolean, true) $$;
        """;
}
