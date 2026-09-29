using Acme.Sistemas.HomeFinance.App.Helpers;
using Acme.Sistemas.HomeFinance.App.Models;
using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App.Pages;

public partial class LancamentosPage : ContentPage
{
	private readonly FinancasApi _api;
	private readonly ContextoTenant _tenant;
	private DateOnly _mes = DateOnly.FromDateTime(DateTime.Today);

	public LancamentosPage(FinancasApi api, ContextoTenant tenant)
	{
		InitializeComponent();
		_api = api;
		_tenant = tenant;
	}

	private sealed record LancamentoView(
		Guid Id, string Descricao, string Detalhe, string ValorTexto,
		string Aviso, bool TemAviso, bool Pendente);

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

	private async void AoMesAnterior(object? s, EventArgs e)
	{
		_mes = _mes.AddMonths(-1);
		await CarregarAsync();
	}

	private async void AoProximoMes(object? s, EventArgs e)
	{
		_mes = _mes.AddMonths(1);
		await CarregarAsync();
	}

	private async void AoAlternarVisao(object? s, ToggledEventArgs e)
	{
		LabelVisao.Text = SwitchCaixa.IsToggled ? "Caixa" : "Competência";
		await CarregarAsync();
	}

	private async void AoNovo(object? s, EventArgs e)
	{
		// Espaço somente leitura não registra: nem oferece a tela (a recusa final é do banco).
		if (_tenant.SomenteLeitura)
		{
			await DisplayAlert("Somente leitura", "Este espaço é compartilhado só para consulta.", "OK");
			return;
		}

		await Shell.Current.GoToAsync("lancamento-novo");
	}

	private async Task CarregarAsync()
	{
		LabelMes.Text = Formato.Mes(_mes);
		var primeiro = new DateOnly(_mes.Year, _mes.Month, 1);
		var ultimo = primeiro.AddMonths(1).AddDays(-1);
		var porCaixa = SwitchCaixa.IsToggled;

		try
		{
			var dados = await _api.LancamentosAsync(primeiro, ultimo, porCaixa);
			Lista.ItemsSource = dados.Lancamentos.Select(Mapear).ToList();
		}
		catch (ErroApi erro)
		{
			await DisplayAlert("Lançamentos", erro.Message, "OK");
			Lista.ItemsSource = Array.Empty<LancamentoView>();
		}
		catch
		{
			Lista.ItemsSource = Array.Empty<LancamentoView>();
		}
	}

	private static LancamentoView Mapear(LancamentoItem l)
	{
		var detalhe = $"{Formato.Data(l.DataCompetencia)} · {l.Tipo}"
			+ (l.Parcelamento is null ? string.Empty : $" · {l.Parcelamento}");

		var aviso = l.PendenteDeClassificacao ? "Pendente de classificação"
			: l.Previsto ? "Previsto" : string.Empty;

		return new LancamentoView(
			l.Id, l.Descricao, detalhe, Formato.Moeda(l.Valor),
			aviso, aviso.Length > 0, l.PendenteDeClassificacao);
	}

	private async void AoSelecionar(object? remetente, SelectionChangedEventArgs argumentos)
	{
		if (argumentos.CurrentSelection.FirstOrDefault() is not LancamentoView item)
		{
			return;
		}

		Lista.SelectedItem = null;

		if (_tenant.SomenteLeitura)
		{
			return;
		}

		await ClassificarAsync(item.Id);
	}

	private async Task ClassificarAsync(Guid lancamentoId)
	{
		try
		{
			var categorias = await _api.CategoriasAsync();
			if (categorias.Categorias.Count == 0)
			{
				await DisplayAlert("Classificar", "Cadastre uma categoria antes de classificar.", "OK");
				return;
			}

			var nomes = categorias.Categorias.Select(c => c.Nome).ToArray();
			var escolha = await DisplayActionSheet("Classificar em", "Cancelar", null, nomes);
			if (escolha is null or "Cancelar")
			{
				return;
			}

			var categoria = categorias.Categorias.First(c => c.Nome == escolha);
			var criarRegra = await DisplayAlert(
				"Criar regra?",
				$"Classificar lançamentos futuros parecidos como “{categoria.Nome}” automaticamente?",
				"Criar regra",
				"Só este");

			await _api.ClassificarAsync(lancamentoId, categoria.Id, criarRegra);
			await CarregarAsync();
		}
		catch (ErroApi erro)
		{
			await DisplayAlert("Classificar", erro.Message, "OK");
		}
	}
}
