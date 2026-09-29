using System.Globalization;
using Acme.Sistemas.HomeFinance.App.Helpers;
using Acme.Sistemas.HomeFinance.App.Models;
using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App.Pages;

public partial class RendaPage : ContentPage
{
	private readonly FinancasApi _api;
	private readonly ContextoTenant _tenant;

	public RendaPage(FinancasApi api, ContextoTenant tenant)
	{
		InitializeComponent();
		_api = api;
		_tenant = tenant;
	}

	private sealed record FonteView(string Nome, string Detalhe, string ValorTexto);

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		BotaoAdicionar.IsEnabled = !_tenant.SomenteLeitura;
		await CarregarAsync();
	}

	private async Task CarregarAsync()
	{
		LabelErro.IsVisible = false;
		try
		{
			var fontes = await _api.RendasAsync();
			LabelPrevista.Text = $"Renda prevista: {Formato.Moeda(fontes.RendaPrevista)}";
			Lista.ItemsSource = fontes.Fontes
				.Select(f => new FonteView(
					f.Nome,
					$"Dia {f.DiaRecebimento} · desde {Formato.Data(f.VigenciaInicio)}"
						+ (f.VigenciaFim is { } fim ? $" até {Formato.Data(fim)}" : string.Empty),
					Formato.Moeda(f.Valor)))
				.ToList();
		}
		catch (ErroApi erro)
		{
			Mostrar(erro.Message);
		}
	}

	private async void AoAdicionar(object? remetente, EventArgs argumentos)
	{
		LabelErro.IsVisible = false;

		var nome = EntradaNome.Text?.Trim() ?? string.Empty;
		if (nome.Length == 0)
		{
			Mostrar("Informe o nome.");
			return;
		}

		if (!decimal.TryParse((EntradaValor.Text ?? string.Empty).Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var valor) || valor <= 0)
		{
			Mostrar("Valor inválido.");
			return;
		}

		if (!int.TryParse(EntradaDia.Text, out var dia) || dia is < 1 or > 31)
		{
			Mostrar("Dia do recebimento inválido (1 a 31).");
			return;
		}

		try
		{
			await _api.CadastrarRendaAsync(new { nome, valor, diaRecebimento = dia });
			EntradaNome.Text = EntradaValor.Text = EntradaDia.Text = string.Empty;
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
