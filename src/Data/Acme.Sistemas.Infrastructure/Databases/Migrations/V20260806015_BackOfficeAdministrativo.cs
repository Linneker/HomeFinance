using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations;

/// <summary>
/// Identidade administrativa do back-office e as funções que a operação usa sem enxergar dado
/// financeiro de cliente.
/// </summary>
/// <remarks>
/// O administrador é uma identidade à parte, sem vínculo com tenant nem com a tabela
/// <c>usuario</c> — por isso ganha tabela própria. A tabela fica sob RLS forçada e SEM policy
/// alguma: a aplicação não lê nem escreve <c>administrador</c> diretamente. Todo acesso passa
/// por função <c>SECURITY DEFINER</c> — a de credenciais (o login acontece sem contexto de
/// usuário, como o do usuário final) e a de provisionamento (ação de operação, nunca exposta em
/// endpoint HTTP).
/// <para>
/// As consultas administrativas de conta e a manutenção do catálogo também são funções definer:
/// <c>listar_contas_administrativas</c> devolve só identificação e situação — nenhuma coluna de
/// lançamento, renda, orçamento ou saldo —, e é o único caminho do back-office aos dados de
/// conta. Não existe função que exponha dado financeiro ao administrador, então ele não tem por
/// onde chegar a ele; o restante do isolamento é a RLS por tenant, que devolve vazio para quem
/// não seleciona tenant (o administrador nunca seleciona).
/// </para>
/// </remarks>
public sealed class V20260806015_BackOfficeAdministrativo : MigrationBase
{
    public override long Versao => 20260806015;

    public override string Descricao => "Identidade administrativa do back-office e funções de operação";

    protected override string Sql => """
        CREATE TABLE administrador (
            id          uuid        PRIMARY KEY,
            email       text        NOT NULL UNIQUE,
            nome        text        NOT NULL,
            senha_hash  text        NOT NULL,
            ativo       boolean     NOT NULL DEFAULT true,
            criado_em   timestamptz NOT NULL,
            excluido_em timestamptz NULL
        );

        ALTER TABLE administrador ENABLE ROW LEVEL SECURITY;
        ALTER TABLE administrador FORCE ROW LEVEL SECURITY;

        -- Sem policy e sem grant: a aplicação não toca a tabela direto. Os privilégios padrão
        -- concedidos no schema (V001) são revogados aqui de propósito, para não deixar o hash
        -- de senha do administrador ao alcance de um SELECT do role da aplicação.
        REVOKE ALL ON administrador FROM homefinance_app;

        -- Login administrativo, como o do usuário final, acontece antes de existir contexto:
        -- sem app.usuario_id a RLS devolveria zero linhas. A função definer é o caminho.
        CREATE OR REPLACE FUNCTION credenciais_admin_por_email(p_email text)
        RETURNS TABLE (administrador_id uuid, senha_hash text)
        LANGUAGE sql
        SECURITY DEFINER
        STABLE
        SET search_path = public
        AS $$
            SELECT a.id, a.senha_hash
            FROM administrador a
            WHERE lower(a.email) = lower(p_email)
              AND a.ativo = true
              AND a.excluido_em IS NULL
        $$;

        -- Provisionamento de administrador: ação de operação, executada por ferramenta interna,
        -- nunca por endpoint. É o único caminho de escrita na tabela — o role da aplicação não
        -- tem INSERT/UPDATE direto. Upsert por e-mail para ser idempotente.
        CREATE OR REPLACE FUNCTION provisionar_administrador(
            p_id uuid,
            p_email text,
            p_nome text,
            p_senha_hash text,
            p_criado_em timestamptz)
        RETURNS void
        LANGUAGE sql
        SECURITY DEFINER
        SET search_path = public
        AS $$
            INSERT INTO administrador (id, email, nome, senha_hash, ativo, criado_em)
            VALUES (p_id, lower(p_email), p_nome, p_senha_hash, true, p_criado_em)
            ON CONFLICT (email) DO UPDATE
                SET nome       = EXCLUDED.nome,
                    senha_hash = EXCLUDED.senha_hash,
                    ativo      = true;
        $$;

        -- Relação de contas para o back-office: identificação e situação, sem NENHUM valor
        -- financeiro. LEFT JOIN para a conta sem assinatura ainda aparecer (situação nula).
        -- Contas excluídas pelo titular não entram.
        CREATE OR REPLACE FUNCTION listar_contas_administrativas()
        RETURNS TABLE (
            usuario_id          uuid,
            email               text,
            criado_em           timestamptz,
            assinatura_situacao smallint,
            plano_codigo        text,
            plano_nome          text)
        LANGUAGE sql
        SECURITY DEFINER
        STABLE
        SET search_path = public
        AS $$
            SELECT u.id, u.email, u.criado_em, a.situacao, p.codigo, p.nome
            FROM usuario u
            LEFT JOIN assinatura a ON a.usuario_id = u.id AND a.situacao IN (1, 2, 3)
            LEFT JOIN plano p ON p.id = a.plano_id
            WHERE u.excluido_em IS NULL
            ORDER BY u.criado_em
        $$;

        -- Manutenção do catálogo pela operação. O catálogo tem INSERT/UPDATE revogados do role
        -- da aplicação (V011): a semeadura é por migration, mas o back-office precisa criar e
        -- ajustar plano e cota. Esta função definer é esse caminho, e só o endpoint sob a
        -- política administrativa a alcança. Upsert por código.
        CREATE OR REPLACE FUNCTION salvar_plano(
            p_id uuid,
            p_codigo text,
            p_nome text,
            p_cota int,
            p_preco numeric,
            p_ativo boolean,
            p_criado_em timestamptz)
        RETURNS void
        LANGUAGE sql
        SECURITY DEFINER
        SET search_path = public
        AS $$
            INSERT INTO plano (id, codigo, nome, cota_compartilhamentos, preco_mensal, ativo, criado_em)
            VALUES (p_id, p_codigo, p_nome, p_cota, p_preco, p_ativo, p_criado_em)
            ON CONFLICT (codigo) DO UPDATE
                SET nome                   = EXCLUDED.nome,
                    cota_compartilhamentos = EXCLUDED.cota_compartilhamentos,
                    preco_mensal           = EXCLUDED.preco_mensal,
                    ativo                  = EXCLUDED.ativo;
        $$;

        REVOKE EXECUTE ON FUNCTION credenciais_admin_por_email(text) FROM PUBLIC;
        REVOKE EXECUTE ON FUNCTION provisionar_administrador(uuid, text, text, text, timestamptz) FROM PUBLIC;
        REVOKE EXECUTE ON FUNCTION listar_contas_administrativas() FROM PUBLIC;
        REVOKE EXECUTE ON FUNCTION salvar_plano(uuid, text, text, int, numeric, boolean, timestamptz) FROM PUBLIC;

        GRANT EXECUTE ON FUNCTION credenciais_admin_por_email(text) TO homefinance_app;
        GRANT EXECUTE ON FUNCTION provisionar_administrador(uuid, text, text, text, timestamptz) TO homefinance_app;
        GRANT EXECUTE ON FUNCTION listar_contas_administrativas() TO homefinance_app;
        GRANT EXECUTE ON FUNCTION salvar_plano(uuid, text, text, int, numeric, boolean, timestamptz) TO homefinance_app;
        """;
}
