using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Usuario.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Usuario;

/// <summary>
/// Escrita de conta — e as leituras que decidem essa escrita, todas pela conexão primária.
/// </summary>
/// <remarks>
/// <see cref="ExisteEmailAsync"/> roda aqui e não no repositório de leitura porque decide uma
/// escrita: contra réplica atrasada, a verificação não enxergaria uma conta criada segundos
/// antes e o sistema gravaria duplicata. Blueprint Acme 2.1.0.
/// </remarks>
public sealed class UsuarioWriteRepository(TransactionManager transacoes) : IUsuarioWriteRepository
{
    public async Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(UsuarioQuery.ExisteEmail)
            .Com("email", email);

        return await comando.ExecuteScalarAsync(cancellationToken) is true;
    }

    public async Task<CredenciaisDeAcesso?> ObterCredenciaisAsync(
        string email,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(UsuarioQuery.ObterCredenciais)
            .Com("email", email);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        if (!await leitor.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CredenciaisDeAcesso(
            leitor.Identificador("usuario_id"),
            leitor.TextoObrigatorio("senha_hash"),
            leitor.Identificador("tenant_id"));
    }

    public async Task CriarContaAsync(
        UsuarioEntity usuario,
        TenantEntity tenant,
        IReadOnlyList<ConsentimentoEntity> consentimentos,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        var sql = new EscopoSql(escopo.Conexao, escopo.Transacao);

        await using (var comando = sql.Comando(UsuarioCommand.Inserir)
                         .Com("id", usuario.Id)
                         .Com("email", usuario.Email)
                         .Com("senhaHash", usuario.SenhaHash)
                         .Com("criadoEm", usuario.CriadoEm.UtcDateTime))
        {
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var comando = sql.Comando(UsuarioCommand.InserirTenant)
                         .Com("id", tenant.Id)
                         .Com("usuarioId", tenant.UsuarioId)
                         .Com("criadoEm", tenant.CriadoEm.UtcDateTime))
        {
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var consentimento in consentimentos)
        {
            await using var comando = sql.Comando(UsuarioCommand.InserirConsentimento)
                .Com("id", consentimento.Id)
                .Com("usuarioId", consentimento.UsuarioId)
                .Com("documento", consentimento.Documento)
                .Com("versao", consentimento.Versao)
                .Com("aceitoEm", consentimento.AceitoEm.UtcDateTime);

            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        // Conta, tenant e consentimento num commit só: conta sem tenant não guarda nada, e
        // dado pessoal tratado sem consentimento registrado é violação da LGPD.
        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task RegistrarConsentimentoAsync(
        ConsentimentoEntity consentimento,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(UsuarioCommand.InserirConsentimento)
            .Com("id", consentimento.Id)
            .Com("usuarioId", consentimento.UsuarioId)
            .Com("documento", consentimento.Documento)
            .Com("versao", consentimento.Versao)
            .Com("aceitoEm", consentimento.AceitoEm.UtcDateTime);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task ExcluirContaAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        var sql = new EscopoSql(escopo.Conexao, escopo.Transacao);

        // Revogar antes de excluir, na mesma transação: se a exclusão falhar, ninguém perde
        // acesso indevidamente; se der certo, ninguém mantém acesso a dado que já não existe.
        await using (var comando = sql.Comando(UsuarioCommand.RevogarCompartilhamentosConcedidos)
                         .Com("usuarioId", usuarioId)
                         .Com("revogadoEm", DateTime.UtcNow))
        {
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var comando = sql.Comando(UsuarioCommand.ExcluirUsuario).Com("usuarioId", usuarioId))
        {
            await comando.ExecuteNonQueryAsync(cancellationToken);
        }

        await escopo.ConfirmarAsync(cancellationToken);
    }
}
