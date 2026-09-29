using Acme.Sistemas.Domain.Entities.Assinaturas;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Assinatura.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Assinatura;

/// <inheritdoc cref="IPlanoWriteRepository"/>
public sealed class PlanoWriteRepository(TransactionManager transacoes) : IPlanoWriteRepository
{
    public async Task SalvarAsync(PlanoEntity plano, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AssinaturaQuery.SalvarPlano)
            .Com("id", plano.Id)
            .Com("codigo", plano.Codigo)
            .Com("nome", plano.Nome)
            .Com("cota", plano.CotaCompartilhamentos)
            .Com("preco", plano.PrecoMensal)
            .Com("ativo", plano.Ativo)
            .Com("criadoEm", plano.CriadoEm.UtcDateTime);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }
}
