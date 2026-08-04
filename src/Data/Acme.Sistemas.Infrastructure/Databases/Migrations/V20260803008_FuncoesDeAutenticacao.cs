using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations;

/// <summary>
/// Aberturas controladas na RLS para o que acontece antes de existir usuário autenticado.
/// </summary>
/// <remarks>
/// Login e verificação de e-mail em uso precisam consultar <c>usuario</c> sem que
/// <c>app.usuario_id</c> esteja definido — por definição, ainda não há quem autenticar. Sob a
/// policy normal essas consultas voltariam vazias: o login falharia sempre, e a checagem de
/// unicidade sempre diria "livre", deixando a duplicata ser barrada só pelo índice único, sem
/// mensagem útil.
/// <para>
/// A saída é <c>SECURITY DEFINER</c>: duas funções estreitas, que devolvem apenas o necessário
/// e nada mais. É um buraco na RLS — por isso ele é explícito, auditável e do tamanho exato do
/// problema, em vez de uma policy permissiva que abriria a tabela inteira.
/// </para>
/// </remarks>
public sealed class V20260803008_FuncoesDeAutenticacao : MigrationBase
{
    public override long Versao => 20260803008;

    public override string Descricao => "Funções SECURITY DEFINER para login e checagem de e-mail";

    protected override string Sql => """
        -- Devolve o mínimo para autenticar: identificador, hash e tenant padrão.
        -- Não expõe e-mail, datas nem qualquer outro campo.
        CREATE OR REPLACE FUNCTION credenciais_por_email(p_email text)
        RETURNS TABLE (usuario_id uuid, senha_hash text, tenant_id uuid)
        LANGUAGE sql
        SECURITY DEFINER
        STABLE
        SET search_path = public
        AS $$
            SELECT u.id, u.senha_hash, t.id
            FROM usuario u
            JOIN tenant t ON t.usuario_id = u.id
            WHERE lower(u.email) = lower(p_email)
              AND u.excluido_em IS NULL
        $$;

        -- Devolve apenas um booleano: não confirma nada sobre a conta existente, o que
        -- limita a enumeração de usuários ao que a tela de cadastro já revelaria.
        CREATE OR REPLACE FUNCTION email_em_uso(p_email text)
        RETURNS boolean
        LANGUAGE sql
        SECURITY DEFINER
        STABLE
        SET search_path = public
        AS $$
            SELECT EXISTS (
                SELECT 1 FROM usuario
                WHERE lower(email) = lower(p_email)
                  AND excluido_em IS NULL
            )
        $$;

        -- Ninguém executa por padrão; só o role da aplicação.
        REVOKE EXECUTE ON FUNCTION credenciais_por_email(text) FROM PUBLIC;
        REVOKE EXECUTE ON FUNCTION email_em_uso(text) FROM PUBLIC;

        GRANT EXECUTE ON FUNCTION credenciais_por_email(text) TO homefinance_app;
        GRANT EXECUTE ON FUNCTION email_em_uso(text) TO homefinance_app;
        """;
}
