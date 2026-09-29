using Acme.Sistemas.HomeFinance.App.Helpers;
using Acme.Sistemas.HomeFinance.App.Models;
using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App.Pages;

public partial class PlanosPage : ContentPage
{
	private readonly FinancasApi _api;
	private readonly ICompraApp _compra;

	public PlanosPage(FinancasApi api, ICompraApp compra)
	{
		InitializeComponent();
		_api = api;
		_compra = compra;
	}

	private sealed record PlanoView(string Codigo, string Nome, string DescricaoCota, string PrecoTexto);

	private static readonly (string Rotulo, CanalPagamento Canal)[] Canais =
	[
		("App Store", CanalPagamento.AppStore),
		("Google Play", CanalPagamento.GooglePlay),
		("Mercado Pago", CanalPagamento.MercadoPago),
		("Asaas", CanalPagamento.Asaas),
	];

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await CarregarAsync();
	}

	private async Task CarregarAsync()
	{
		try
		{
			var catalogo = await _api.PlanosAsync();
			Lista.ItemsSource = catalogo.Planos
				.Select(p => new PlanoView(
					p.Codigo,
					p.Nome,
					p.CotaCompartilhamentos == 0
						? "Sem compartilhamento"
						: $"Até {p.CotaCompartilhamentos} compartilhamento(s)",
					$"{Formato.Moeda(p.PrecoMensal)} / mês"))
				.ToList();
		}
		catch (ErroApi erro)
		{
			Mostrar(erro.Message);
		}
		catch
		{
			Mostrar("Não foi possível carregar os planos.");
		}
	}

	private async void AoContratar(object? remetente, EventArgs argumentos)
	{
		if (remetente is not Button { CommandParameter: string codigo })
		{
			return;
		}

		var escolha = await DisplayActionSheet("Contratar por", "Cancelar", null, [.. Canais.Select(c => c.Rotulo)]);
		if (escolha is null or "Cancelar")
		{
			return;
		}

		var canal = Canais.First(c => c.Rotulo == escolha).Canal;

		Ocupar(true);
		try
		{
			// A compra devolve só o comprovante — nenhum dado de cartão passa pelo app. Quem
			// valida e libera o recurso é o servidor, no POST seguinte.
			var comprovante = await _compra.ComprarAsync(codigo, canal);
			if (comprovante is null)
			{
				Mostrar("Compra cancelada.");
				return;
			}

			await _api.ContratarAsync(codigo, comprovante.Canal, comprovante.Comprovante);
			await DisplayAlert("Assinatura", "Contratação confirmada.", "OK");
			await Shell.Current.GoToAsync("..");
		}
		catch (ErroApi erro)
		{
			Mostrar(erro.Message);
		}
		catch
		{
			Mostrar("Não foi possível concluir a contratação.");
		}
		finally
		{
			Ocupar(false);
		}
	}

	private void Mostrar(string mensagem)
	{
		LabelMensagem.Text = mensagem;
		LabelMensagem.IsVisible = true;
	}

	private void Ocupar(bool ocupado)
	{
		Ocupado.IsRunning = ocupado;
		Ocupado.IsVisible = ocupado;
	}
}
