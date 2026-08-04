using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Repository.Repositories.V1.Categoria;
using Acme.Sistemas.Repository.Repositories.V1.Lancamento;
using Acme.Sistemas.Repository.Repositories.V1.Recorrencia;
using Acme.Sistemas.Repository.Repositories.V1.Tenant;
using Acme.Sistemas.Repository.Repositories.V1.Usuario;
using Microsoft.Extensions.DependencyInjection;

namespace Acme.Sistemas.Repository;

public static class RepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddRepository(this IServiceCollection services)
    {
        services.AddScoped<IUsuarioReadRepository, UsuarioReadRepository>();
        services.AddScoped<IUsuarioWriteRepository, UsuarioWriteRepository>();
        services.AddScoped<ITenantReadRepository, TenantReadRepository>();
        services.AddScoped<ITenantWriteRepository, TenantWriteRepository>();

        services.AddScoped<ICategoriaReadRepository, CategoriaReadRepository>();
        services.AddScoped<ICategoriaWriteRepository, CategoriaWriteRepository>();
        services.AddScoped<ILancamentoReadRepository, LancamentoReadRepository>();
        services.AddScoped<ILancamentoWriteRepository, LancamentoWriteRepository>();
        services.AddScoped<ILancamentoParticipacaoReadRepository, LancamentoParticipacaoReadRepository>();
        services.AddScoped<IRecorrenciaReadRepository, RecorrenciaReadRepository>();
        services.AddScoped<IRecorrenciaWriteRepository, RecorrenciaWriteRepository>();

        return services;
    }
}
