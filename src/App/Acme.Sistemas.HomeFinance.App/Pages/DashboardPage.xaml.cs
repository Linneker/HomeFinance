using Acme.Sistemas.HomeFinance.App.Helpers;
using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App.Pages;

public partial class DashboardPage : ContentPage
{
	private readonly FinancasApi _api;
	private readonly ContextoTenant _tenant;
	private readonly INotificacaoDispositivo _push;

	public DashboardPage(FinancasApi api, ContextoTenant tenant, INotificacaoDispositivo push)
	{
		InitializeComponent();
		_api = api;
		_tenant = tenant;
		_push = push;
		_tenant.Alterado += (_, _) => Dispatcher.Dispatch(async () => await CarregarAsync());
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		// Registro do dispositivo para alertas do titular (idempotente). Ver INotificacaoDispositivo.
		await _push.RegistrarAsync();
		await CarregarAsync();
	}

	private async void AoAtualizar(object? s, EventArgs e)
	{
		await CarregarAsync();
		Atualizar.IsRefreshing = false;
	}

	private async Task CarregarAsync()
	{
		var hoje = DateOnly.FromDateTime(DateTime.Today);
		LabelMes.Text = Formato.Mes(hoje);

		// Barra de somente leitura, sempre visível quando o espaço é de consulta.
		BarraLeitura.IsVisible = _tenant.SomenteLeitura;
		LabelLeitura.Text = _tenant.Rotulo is { } rotulo
			? $"Somente leitura · {rotulo}"
			: "Somente leitura";

		await Task.WhenAll(CarregarOnboardingAsync(), CarregarResumoAsync(hoje), CarregarAlertasAsync(), CarregarAssinaturaAsync());
	}

	private async Task CarregarAssinaturaAsync()
	{
		try
		{
			var s = await _api.SituacaoAssinaturaAsync();
			var alerta = s.Situacao switch
			{
				Models.SituacaoAssinatura.Suspensa =>
					"Assinatura suspensa. Registrar e consultar seguem disponíveis; os recursos derivados ficam indisponíveis até regularizar.",
				Models.SituacaoAssinatura.Carencia =>
					"Assinatura em carência. Regularize o pagamento para não ser suspensa.",
				Models.SituacaoAssinatura.Cancelada =>
					"Assinatura cancelada. Contrate um plano para voltar a ter os recursos.",
				_ => null,
			};

			PainelAssinatura.IsVisible = alerta is not null;
			LabelAssinatura.Text = alerta ?? string.Empty;
		}
		catch
		{
			PainelAssinatura.IsVisible = false;
		}
	}

	private async void AoRegularizar(object? s, EventArgs e) => await Shell.Current.GoToAsync("assinatura");

	private async Task CarregarOnboardingAsync()
	{
		try
		{
			var p = await _api.OnboardingAsync();
			// Estado vazio antes do onboarding: a barra guia, mas a navegação segue liberada.
			PainelOnboarding.IsVisible = !p.Concluido;
			Progresso.Progress = p.PercentualConcluido / 100.0;
			LabelProgresso.Text = $"{p.PercentualConcluido}% concluído";

			PassosPendentes.Clear();
			foreach (var passo in p.Pendentes.Where(x => !x.Futuro).Take(4))
			{
				PassosPendentes.Add(new Label
				{
					Text = $"• {passo.Descricao}",
					FontSize = 13,
				});
			}
		}
		catch
		{
			PainelOnboarding.IsVisible = false;
		}
	}

	private async Task CarregarResumoAsync(DateOnly mes)
	{
		try
		{
			var competencia = new DateOnly(mes.Year, mes.Month, 1);
			var resumo = await _api.ResumoAsync(competencia, porCaixa: false);
			var vazio = resumo.TotalRealizado == 0 && resumo.TotalPlanejado == 0;
			LabelResumoVazio.IsVisible = vazio;
			LabelResumo.IsVisible = !vazio;
			LabelResumo.Text =
				$"Realizado: {Formato.Moeda(resumo.TotalRealizado)}\n" +
				$"Planejado: {Formato.Moeda(resumo.TotalPlanejado)}\n" +
				$"Diferença: {Formato.Moeda(resumo.Diferenca)}";
		}
		catch
		{
			LabelResumo.IsVisible = false;
			LabelResumoVazio.IsVisible = true;
		}
	}

	private async Task CarregarAlertasAsync()
	{
		try
		{
			var alertas = await _api.AlertasAsync();
			LabelAlertas.Text = alertas.Alertas.Count == 0
				? "Nenhum alerta aberto."
				: $"{alertas.Alertas.Count} alerta(s) aberto(s).";
		}
		catch
		{
			LabelAlertas.Text = "—";
		}
	}

	private async void AoVerAlertas(object? s, EventArgs e) => await Shell.Current.GoToAsync("//main/alertas");
}
