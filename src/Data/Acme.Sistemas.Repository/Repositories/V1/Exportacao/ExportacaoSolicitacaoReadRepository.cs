using Acme.Sistemas.Domain.Entities.Exportacao;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Exportacao.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Exportacao;

/// <inheritdoc cref="IExportacaoSolicitacaoReadRepository"/>
public sealed class ExportacaoSolicitacaoReadRepository(TransactionManager transacoes)
    : IExportacaoSolicitacaoReadRepository
{
    public async Task<ExportacaoSolicitacaoEntity?> ObterAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(ExportacaoSolicitacaoQuery.PorId)
            .Com("id", id);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        return await leitor.ReadAsync(cancellationToken)
            ? ExportacaoSolicitacaoMap.Ler(leitor)
            : null;
    }
}
