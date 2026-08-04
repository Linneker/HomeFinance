namespace Acme.Sistemas.Repository.Repositories.V1.Usuario.Query;

/// <summary>INSERT, UPDATE e DELETE de usuário, centralizados.</summary>
internal static class UsuarioCommand
{
    public const string Inserir = """
        INSERT INTO usuario (id, email, senha_hash, criado_em)
        VALUES (@id, @email, @senhaHash, @criadoEm)
        """;

    public const string InserirTenant = """
        INSERT INTO tenant (id, usuario_id, criado_em)
        VALUES (@id, @usuarioId, @criadoEm)
        """;

    public const string InserirConsentimento = """
        INSERT INTO consentimento (id, usuario_id, documento, versao, aceito_em)
        VALUES (@id, @usuarioId, @documento, @versao, @aceitoEm)
        """;

    /// <summary>
    /// Encerra os compartilhamentos concedidos pelo tenant da conta. Roda antes da exclusão
    /// para que os destinatários percam o acesso na mesma transação.
    /// </summary>
    public const string RevogarCompartilhamentosConcedidos = """
        UPDATE tenant_compartilhamento
        SET revogado_em = @revogadoEm
        WHERE revogado_em IS NULL
          AND tenant_id IN (SELECT id FROM tenant WHERE usuario_id = @usuarioId)
        """;

    /// <summary>
    /// A exclusão do usuário cascateia para tenant, consentimento, compartilhamento e todo o
    /// dado financeiro — as chaves estrangeiras são ON DELETE CASCADE.
    /// </summary>
    public const string ExcluirUsuario = """
        DELETE FROM usuario WHERE id = @usuarioId
        """;
}
