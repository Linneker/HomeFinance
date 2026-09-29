using Acme.Sistemas.HomeFinance.App.Models;
using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App.Pages;

public partial class RegrasPage : ContentPage
{
	private readonly FinancasApi _api;
	private readonly ContextoTenant _tenant;

	public RegrasPage(FinancasApi api, ContextoTenant tenant)
	{
		InitializeComponent();
		_api = api;
		_tenant = tenant;
	}

	private sealed record RegraView(Guid Id, string Padrao, string CategoriaNome);

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await CarregarAsync();
	}

	private async Task CarregarAsync()
	{
		LabelErro.IsVisible = false;
		try
		{
			var categorias = (await _api.CategoriasAsync()).Categorias;
			ListaCategorias.ItemsSource = categorias;

			var regras = await _api.RegrasAsync();
			ListaRegras.ItemsSource = regras.Regras
				.Select(r => new RegraView(
					r.Id,
					r.Padrao,
					categorias.FirstOrDefault(c => c.Id == r.CategoriaId)?.Nome ?? "Categoria"))
				.ToList();
		}
		catch (ErroApi erro)
		{
			Mostrar(erro.Message);
		}
	}

	private async void AoAdicionarCategoria(object? remetente, EventArgs argumentos)
	{
		if (_tenant.SomenteLeitura)
		{
			await DisplayAlert("Somente leitura", "Este espaço é compartilhado só para consulta.", "OK");
			return;
		}

		var nome = EntradaCategoria.Text?.Trim() ?? string.Empty;
		if (nome.Length == 0)
		{
			return;
		}

		try
		{
			await _api.CriarCategoriaAsync(nome);
			EntradaCategoria.Text = string.Empty;
			await CarregarAsync();
		}
		catch (ErroApi erro)
		{
			Mostrar(erro.Message);
		}
	}

	private async void AoExcluirRegra(object? remetente, EventArgs argumentos)
	{
		if (remetente is not Button { CommandParameter: Guid id })
		{
			return;
		}

		try
		{
			await _api.ExcluirRegraAsync(id);
			await CarregarAsync();
		}
		catch (ErroApi erro)
		{
			Mostrar(erro.Message);
		}
	}

	private void Mostrar(string mensagem)
	{
		LabelErro.Text = mensagem;
		LabelErro.IsVisible = true;
	}
}
