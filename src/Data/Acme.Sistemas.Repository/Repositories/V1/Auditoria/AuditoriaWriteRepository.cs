using Acme.Sistemas.Domain.Entities.Auditoria;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Auditoria.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Auditoria;

/// <inheritdoc cref="IAuditoriaRepository"/>
/// <remarks>
/// Só INSERT: a tabela é append-only e o app não tem grant para lê-la. Escreve pela conexão de
/// escrita porque é um registro que precisa estar no primário no mesmo instante do evento que o
/// gerou.
/// </remarks>
public sealed class AuditoriaWriteRepository(TransactionManager transacoes) : IAuditoriaRepository
{
    public async Task RegistrarAsync(
        RegistroAuditoriaEntity registro,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AuditoriaCommand.Registrar)
            .Com("id", registro.Id)
            .Com("evento", (short)registro.Evento)
            .Com("contaUsuarioId", registro.ContaUsuarioId)
            .Com("autorId", registro.AutorId)
            .Com("detalhe", registro.Detalhe)
            .Com("criadoEm", registro.CriadoEm.UtcDateTime);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }
}
