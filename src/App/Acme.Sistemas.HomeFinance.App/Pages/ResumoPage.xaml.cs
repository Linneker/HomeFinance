using Acme.Sistemas.HomeFinance.App.Helpers;
using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App.Pages;

public partial class ResumoPage : ContentPage
{
	private readonly FinancasApi _api;
	private DateOnly _mes = DateOnly.FromDateTime(DateTime.Today);

	public ResumoPage(FinancasApi api)
	{
		InitializeComponent();
		_api = api;
	}

	private sealed record LinhaView(string Nome, string Detalhe, string Realizado, Color Cor);

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

	private async void AoMesAnterior(object? s, EventArgs e) { _mes = _mes.AddMonths(-1); await CarregarAsync(); }
	private async void AoProximoMes(object? s, EventArgs e) { _mes = _mes.AddMonths(1); await CarregarAsync(); }

	private async void AoAlternar(object? s, ToggledEventArgs e)
	{
		LabelVisao.Text = SwitchCaixa.IsToggled ? "Caixa" : "Competência";
		await CarregarAsync();
	}

	private async Task CarregarAsync()
	{
		LabelMes.Text = Formato.Mes(_mes);
		var competencia = new DateOnly(_mes.Year, _mes.Month, 1);

		try
		{
			var categorias = (await _api.CategoriasAsync()).Categorias;
			var resumo = await _api.ResumoAsync(competencia, SwitchCaixa.IsToggled);

			string Nome(Guid id) => categorias.FirstOrDefault(c => c.Id == id)?.Nome ?? "Categoria";

			var linhas = resumo.Categorias
				.Select(l => new LinhaView(
					Nome(l.CategoriaId),
					$"Planejado {Formato.Moeda(l.Planejado)} · {l.Quantidade} lançamento(s)",
					Formato.Moeda(l.Realizado),
					l.Excedida ? Colors.OrangeRed : Colors.Green))
				.ToList();

			if (resumo.ForaDoPlanejamento.Total > 0)
			{
				linhas.Add(new LinhaView(
					"Fora do planejamento",
					$"Inclui {Formato.Moeda(resumo.ForaDoPlanejamento.PendenteDeClassificacao)} pendente de classificação",
					Formato.Moeda(resumo.ForaDoPlanejamento.Total),
					Colors.Gray));
			}

			Lista.ItemsSource = linhas;
			LabelTotais.Text =
				$"Planejado {Formato.Moeda(resumo.TotalPlanejado)} · Realizado {Formato.Moeda(resumo.TotalRealizado)} · Diferença {Formato.Moeda(resumo.Diferenca)}";
		}
		catch (ErroApi erro)
		{
			await DisplayAlert("Resumo", erro.Message, "OK");
		}
	}
}
