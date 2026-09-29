using Acme.Sistemas.HomeFinance.App.Models;
using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App.Pages;

public partial class TenantsPage : ContentPage
{
	private readonly FinancasApi _api;
	private readonly ContextoTenant _contexto;

	public TenantsPage(FinancasApi api, ContextoTenant contexto)
	{
		InitializeComponent();
		_api = api;
		_contexto = contexto;
	}

	private sealed record TenantView(Guid TenantId, string Titulo, string Subtitulo, string Marca, bool SomenteLeitura);

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await CarregarAsync();
	}

	private async void AoAtualizar(object? s, EventArgs e)
	{
		await CarregarAsync();
		Atualizar.IsRefreshing = false;
	}

	private async Task CarregarAsync()
	{
		try
		{
			var tenants = await _api.TenantsAsync();
			Lista.ItemsSource = tenants.Tenants
				.Select(t => new TenantView(
					t.TenantId,
					t.EhProprio ? "Meu espaço" : "Espaço compartilhado",
					t.SomenteLeitura ? "Somente leitura" : "Leitura e escrita",
					_contexto.TenantId == t.TenantId ? "✓ atual" : string.Empty,
					t.SomenteLeitura))
				.ToList();
		}
		catch (ErroApi erro)
		{
			await DisplayAlert("Espaços", erro.Message, "OK");
		}
	}

	private async void AoSelecionar(object? remetente, SelectionChangedEventArgs argumentos)
	{
		if (argumentos.CurrentSelection.FirstOrDefault() is not TenantView escolhido)
		{
			return;
		}

		Lista.SelectedItem = null;
		_contexto.Selecionar(escolhido.TenantId, escolhido.Titulo, escolhido.SomenteLeitura);
		await Shell.Current.GoToAsync("//main/dashboard");
	}
}
