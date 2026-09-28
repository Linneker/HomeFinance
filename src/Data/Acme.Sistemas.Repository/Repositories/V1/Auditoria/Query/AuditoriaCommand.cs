namespace Acme.Sistemas.Repository.Repositories.V1.Auditoria.Query;

internal static class AuditoriaCommand
{
    public const string Registrar = """
        INSERT INTO auditoria (id, evento, conta_usuario_id, autor_id, detalhe, criado_em)
        VALUES (@id, @evento, @contaUsuarioId, @autorId, @detalhe, @criadoEm)
        """;
}
