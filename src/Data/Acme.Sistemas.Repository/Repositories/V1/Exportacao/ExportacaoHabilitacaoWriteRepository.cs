using Acme.Sistemas.Domain.Entities.Exportacao;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Exportacao.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Exportacao;

/// <inheritdoc cref="IExportacaoHabilitacaoWriteRepository"/>
public sealed class ExportacaoHabilitacaoWriteRepository(TransactionManager transacoes)
    : IExportacaoHabilitacaoWriteRepository
{
    public async Task DefinirAsync(
        ExportacaoHabilitacaoEntity habilitacao,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(ExportacaoHabilitacaoSql.Definir)
            .Com("id", habilitacao.Id)
            .Com("usuarioId", habilitacao.UsuarioId)
            .Com("habilitada", habilitacao.Habilitada)
            .Com("motivo", habilitacao.Motivo)
            .Com("autorId", habilitacao.AutorId)
            .Com("quando", habilitacao.CriadoEm.UtcDateTime);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task<bool> HabilitadaAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(ExportacaoHabilitacaoSql.Habilitada)
            .Com("usuarioId", usuarioId);

        return await comando.ExecuteScalarAsync(cancellationToken) is true;
    }
}
