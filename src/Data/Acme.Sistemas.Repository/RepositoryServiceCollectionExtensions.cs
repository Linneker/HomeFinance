using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Repository.Repositories.V1.Alerta;
using Acme.Sistemas.Repository.Repositories.V1.Assinatura;
using Acme.Sistemas.Repository.Repositories.V1.Auditoria;
using Acme.Sistemas.Repository.Repositories.V1.Categoria;
using Acme.Sistemas.Repository.Repositories.V1.DocumentoRetido;
using Acme.Sistemas.Repository.Repositories.V1.Exportacao;
using Acme.Sistemas.Repository.Repositories.V1.FonteRenda;
using Acme.Sistemas.Repository.Repositories.V1.Lancamento;
using Acme.Sistemas.Repository.Repositories.V1.Onboarding;
using Acme.Sistemas.Repository.Repositories.V1.Orcamento;
using Acme.Sistemas.Repository.Repositories.V1.Recorrencia;
using Acme.Sistemas.Repository.Repositories.V1.RegraClassificacao;
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
        services.AddScoped<IRegraClassificacaoReadRepository, RegraClassificacaoReadRepository>();
        services.AddScoped<IRegraClassificacaoWriteRepository, RegraClassificacaoWriteRepository>();

        services.AddScoped<IFonteRendaReadRepository, FonteRendaReadRepository>();
        services.AddScoped<IFonteRendaWriteRepository, FonteRendaWriteRepository>();
        services.AddScoped<IOrcamentoReadRepository, OrcamentoReadRepository>();
        services.AddScoped<IOrcamentoWriteRepository, OrcamentoWriteRepository>();

        services.AddScoped<IRelatorioReadRepository, RelatorioReadRepository>();

        services.AddScoped<IAlertaReadRepository, AlertaReadRepository>();
        services.AddScoped<IAlertaWriteRepository, AlertaWriteRepository>();
        services.AddScoped<IRotinaReadRepository, RotinaReadRepository>();

        services.AddScoped<IOnboardingReadRepository, OnboardingReadRepository>();
        services.AddScoped<IOnboardingWriteRepository, OnboardingWriteRepository>();

        services.AddScoped<AssinaturaReadRepository>();
        services.AddScoped<IAssinaturaReadRepository>(sp => sp.GetRequiredService<AssinaturaReadRepository>());
        services.AddScoped<IPlanoReadRepository>(sp => sp.GetRequiredService<AssinaturaReadRepository>());
        services.AddScoped<IAssinaturaWriteRepository, AssinaturaWriteRepository>();
        services.AddScoped<IDocumentoRetidoRepository, DocumentoRetidoRepository>();

        services.AddScoped<IExportacaoHabilitacaoReadRepository, ExportacaoHabilitacaoReadRepository>();
        services.AddScoped<IExportacaoHabilitacaoWriteRepository, ExportacaoHabilitacaoWriteRepository>();
        services.AddScoped<IExportacaoSolicitacaoReadRepository, ExportacaoSolicitacaoReadRepository>();
        services.AddScoped<IExportacaoSolicitacaoWriteRepository, ExportacaoSolicitacaoWriteRepository>();
        services.AddScoped<IExportacaoDadosReadRepository, ExportacaoDadosReadRepository>();
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();

        return services;
    }
}
