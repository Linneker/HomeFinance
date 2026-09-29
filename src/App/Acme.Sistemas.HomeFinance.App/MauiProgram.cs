using Acme.Sistemas.HomeFinance.App.Pages;
using Acme.Sistemas.HomeFinance.App.Services;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.HomeFinance.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// ---- serviços ----
		builder.Services.AddSingleton<ApiConfig>();
		builder.Services.AddSingleton<ISessao, Sessao>();
		builder.Services.AddSingleton<ContextoTenant>();
		builder.Services.AddSingleton(sp =>
			new HttpClient { BaseAddress = new Uri(sp.GetRequiredService<ApiConfig>().BaseUrl) });
		builder.Services.AddSingleton<ApiClient>();
		builder.Services.AddSingleton<IdentidadeApi>();
		builder.Services.AddSingleton<FinancasApi>();
		builder.Services.AddSingleton<ICompraApp, CompraAppSimulada>();
		builder.Services.AddSingleton<INotificacaoDispositivo, NotificacaoDispositivoStub>();

		// ---- navegação e telas ----
		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<CriarContaPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<LancamentosPage>();
		builder.Services.AddTransient<OrcamentoPage>();
		builder.Services.AddTransient<AlertasPage>();
		builder.Services.AddTransient<MaisPage>();
		builder.Services.AddTransient<AssinaturaPage>();
		builder.Services.AddTransient<PlanosPage>();
		builder.Services.AddTransient<CriarLancamentoPage>();
		builder.Services.AddTransient<RendaPage>();
		builder.Services.AddTransient<RegrasPage>();
		builder.Services.AddTransient<CompartilhamentoPage>();
		builder.Services.AddTransient<ExportacaoPage>();
		builder.Services.AddTransient<TenantsPage>();
		builder.Services.AddTransient<ResumoPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
