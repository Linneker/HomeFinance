using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Exportacao.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Exportacao;

/// <inheritdoc cref="IExportacaoHabilitacaoReadRepository"/>
public sealed class ExportacaoHabilitacaoReadRepository(TransactionManager transacoes)
    : IExportacaoHabilitacaoReadRepository
{
    public async Task<bool> HabilitadaAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(ExportacaoHabilitacaoSql.Habilitada)
            .Com("usuarioId", usuarioId);

        return await comando.ExecuteScalarAsync(cancellationToken) is true;
    }
}
