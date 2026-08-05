using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Onboarding;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Onboarding.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Onboarding;

public sealed class OnboardingReadRepository(TransactionManager transacoes) : IOnboardingReadRepository
{
    public async Task<IReadOnlyList<OnboardingRequisitoEntity>> CatalogoAsync(
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(OnboardingQuery.Catalogo);

        return await LerCatalogoAsync(comando, cancellationToken);
    }

    public async Task<IReadOnlyList<OnboardingEstadoEntity>> EstadoDoTenantAsync(
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(OnboardingQuery.EstadoDoTenant);

        return await LerEstadoAsync(comando, cancellationToken);
    }

    internal static async Task<IReadOnlyList<OnboardingRequisitoEntity>> LerCatalogoAsync(
        DbCommand comando,
        CancellationToken cancellationToken)
    {
        var requisitos = new List<OnboardingRequisitoEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            requisitos.Add(new OnboardingRequisitoEntity
            {
                Codigo = leitor.TextoObrigatorio("codigo"),
                Descricao = leitor.TextoObrigatorio("descricao"),
                Ordem = leitor.GetInt32(leitor.GetOrdinal("ordem")),
                Ativo = leitor.GetBoolean(leitor.GetOrdinal("ativo")),
                Bloqueante = leitor.GetBoolean(leitor.GetOrdinal("bloqueante")),
            });
        }

        return requisitos;
    }

    internal static async Task<IReadOnlyList<OnboardingEstadoEntity>> LerEstadoAsync(
        DbCommand comando,
        CancellationToken cancellationToken)
    {
        var estados = new List<OnboardingEstadoEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            estados.Add(new OnboardingEstadoEntity
            {
                Id = leitor.Identificador("id"),
                TenantId = leitor.Identificador("tenant_id"),
                RequisitoCodigo = leitor.TextoObrigatorio("requisito_codigo"),
                CumpridoEm = leitor.DataHoraOpcional("cumprido_em"),
            });
        }

        return estados;
    }
}
