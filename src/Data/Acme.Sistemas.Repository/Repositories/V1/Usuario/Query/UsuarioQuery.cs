namespace Acme.Sistemas.Repository.Repositories.V1.Usuario.Query;

/// <summary>
/// SELECTs de usuário, centralizados. Sem ORM, o SQL é o contrato real com o banco — mantê-lo
/// num só lugar é o que o torna auditável.
/// </summary>
/// <remarks>
/// O mesmo texto pode ser executado pelos dois repositórios. O que separa leitura de escrita é
/// qual conexão executa, não onde o SQL está guardado.
/// </remarks>
internal static class UsuarioQuery
{
    public const string ObterPorId = """
        SELECT id, email, senha_hash, criado_em, excluido_em
        FROM usuario
        WHERE id = @id
        """;

    // Login e checagem de unicidade acontecem antes de haver usuário autenticado, quando a
    // policy de RLS de `usuario` devolveria zero linhas. Estas duas funções SECURITY DEFINER
    // são a abertura controlada para essa borda — ver migration 20260803008.
    public const string ObterCredenciais = """
        SELECT usuario_id, senha_hash, tenant_id FROM credenciais_por_email(@email)
        """;

    public const string ExisteEmail = """
        SELECT email_em_uso(@email)
        """;

    public const string ObterConsentimentos = """
        SELECT id, usuario_id, documento, versao, aceito_em
        FROM consentimento
        WHERE usuario_id = @usuarioId
        ORDER BY aceito_em DESC
        """;
}
