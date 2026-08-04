using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Usuario.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Usuario;

/// <summary>Leituras que atendem Queries — pela conexão de leitura.</summary>
public sealed class UsuarioReadRepository(TransactionManager transacoes) : IUsuarioReadRepository
{
    public async Task<UsuarioEntity?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(UsuarioQuery.ObterPorId)
            .Com("id", id);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        return await leitor.ReadAsync(cancellationToken) ? Materializar(leitor) : null;
    }

    public async Task<IReadOnlyList<ConsentimentoEntity>> ObterConsentimentosAsync(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(UsuarioQuery.ObterConsentimentos)
            .Com("usuarioId", usuarioId);

        var consentimentos = new List<ConsentimentoEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            consentimentos.Add(new ConsentimentoEntity
            {
                Id = leitor.Identificador("id"),
                UsuarioId = leitor.Identificador("usuario_id"),
                Documento = leitor.TextoObrigatorio("documento"),
                Versao = leitor.TextoObrigatorio("versao"),
                AceitoEm = leitor.DataHora("aceito_em"),
                CriadoEm = leitor.DataHora("aceito_em"),
            });
        }

        return consentimentos;
    }

    internal static UsuarioEntity Materializar(System.Data.Common.DbDataReader leitor) => new()
    {
        Id = leitor.Identificador("id"),
        Email = leitor.TextoObrigatorio("email"),
        SenhaHash = leitor.TextoObrigatorio("senha_hash"),
        CriadoEm = leitor.DataHora("criado_em"),
        ExcluidoEm = leitor.DataHoraOpcional("excluido_em"),
    };
}
