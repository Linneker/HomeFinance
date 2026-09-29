using Acme.Sistemas.HomeFinance.App.Helpers;
using Acme.Sistemas.HomeFinance.App.Models;
using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App.Pages;

public partial class AlertasPage : ContentPage
{
	private readonly FinancasApi _api;

	public AlertasPage(FinancasApi api)
	{
		InitializeComponent();
		_api = api;
	}

	// Cores distintas para os dois tipos, para a pessoa diferenciar de relance (13.14).
	private static readonly Color CorCompetencia = Color.FromArgb("#FFF4E5"); // âmbar suave
	private static readonly Color CorCaixa = Color.FromArgb("#E7F0FF");       // azul suave

	private sealed record AlertaView(Guid Id, string TipoTexto, string Mensagem, string Quando, Color Cor);

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await CarregarAsync();
	}

	private async void AoAtualizar(object? remetente, EventArgs argumentos)
	{
		await CarregarAsync();
		Atualizar.IsRefreshing = false;
	}

	private async Task CarregarAsync()
	{
		try
		{
			var alertas = await _api.AlertasAsync();
			Lista.ItemsSource = alertas.Alertas
				.Select(a => new AlertaView(
					a.Id,
					Formato.Alerta(a.Tipo),
					a.Mensagem,
					Formato.DataHora(a.CriadoEm),
					a.Tipo == TipoAlerta.Competencia ? CorCompetencia : CorCaixa))
				.ToList();
		}
		catch
		{
			// Silencioso: a central de alertas não deve bloquear a navegação se a carga falhar.
			Lista.ItemsSource = Array.Empty<AlertaView>();
		}
	}

	private async void AoDispensar(object? remetente, EventArgs argumentos)
	{
		if (remetente is not Button { CommandParameter: Guid id })
		{
			return;
		}

		try
		{
			await _api.DispensarAlertaAsync(id);
			await CarregarAsync();
		}
		catch (ErroApi erro)
		{
			await DisplayAlert("Alertas", erro.Message, "OK");
		}
	}
}
