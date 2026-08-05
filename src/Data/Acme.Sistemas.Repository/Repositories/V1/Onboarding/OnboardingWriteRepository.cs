using Acme.Sistemas.Domain.Entities.Onboarding;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Onboarding.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Onboarding;

public sealed class OnboardingWriteRepository(TransactionManager transacoes)
    : IOnboardingWriteRepository
{
    public async Task<IReadOnlyList<OnboardingRequisitoEntity>> CatalogoAsync(
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(OnboardingQuery.Catalogo);

        return await OnboardingReadRepository.LerCatalogoAsync(comando, cancellationToken);
    }

    public async Task<IReadOnlyList<OnboardingEstadoEntity>> EstadoDoTenantAsync(
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(OnboardingQuery.EstadoDoTenant);

        return await OnboardingReadRepository.LerEstadoAsync(comando, cancellationToken);
    }

    public async Task MarcarCumpridoAsync(
        OnboardingEstadoEntity estado,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(OnboardingCommand.MarcarCumprido)
            .Com("id", estado.Id)
            .Com("tenantId", estado.TenantId)
            .Com("requisitoCodigo", estado.RequisitoCodigo)
            .Com("cumpridoEm", estado.CumpridoEm?.UtcDateTime);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }
}
