namespace Acme.Sistemas.Repository.Repositories.V1.Administracao.Query;

/// <summary>
/// SQL da administração. Tudo passa por função <c>SECURITY DEFINER</c>: a tabela
/// <c>administrador</c> não é acessível pelo role da aplicação, e a relação de contas projeta só
/// colunas não financeiras.
/// </summary>
internal static class AdministracaoSql
{
    public const string CredenciaisPorEmail =
        "SELECT administrador_id, senha_hash FROM credenciais_admin_por_email(@email)";

    public const string Provisionar =
        "SELECT provisionar_administrador(@id, @email, @nome, @senhaHash, @criadoEm)";

    public const string ListarContas =
        "SELECT usuario_id, email, criado_em, assinatura_situacao, plano_codigo, plano_nome "
        + "FROM listar_contas_administrativas()";
}
