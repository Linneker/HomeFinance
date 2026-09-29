using System.Globalization;
using Acme.Sistemas.HomeFinance.App.Helpers;
using Acme.Sistemas.HomeFinance.App.Models;
using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App.Pages;

public partial class OrcamentoPage : ContentPage
{
	private readonly FinancasApi _api;
	private readonly ContextoTenant _tenant;
	private DateOnly _mes = DateOnly.FromDateTime(DateTime.Today);
	private IReadOnlyList<CategoriaItem> _categorias = [];

	public OrcamentoPage(FinancasApi api, ContextoTenant tenant)
	{
		InitializeComponent();
		_api = api;
		_tenant = tenant;
	}

	private sealed record LinhaView(Guid CategoriaId, string Nome, string ValorTexto);

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

	private async Task CarregarAsync()
	{
		LabelMes.Text = Formato.Mes(_mes);
		var competencia = new DateOnly(_mes.Year, _mes.Month, 1);
		BotaoCopiar.IsVisible = !_tenant.SomenteLeitura;

		try
		{
			_categorias = (await _api.CategoriasAsync()).Categorias;
			var orcamento = await _api.OrcamentoAsync(competencia);

			Lista.ItemsSource = orcamento.Categorias
				.Select(o => new LinhaView(o.CategoriaId, NomeCategoria(o.CategoriaId), Formato.Moeda(o.ValorPlanejado)))
				.ToList();

			LabelTotais.Text = $"Planejado: {Formato.Moeda(orcamento.TotalPlanejado)} · Renda prevista: {Formato.Moeda(orcamento.RendaPrevista)}";
			LabelAviso.Text = orcamento.Aviso ?? string.Empty;
			LabelAviso.IsVisible = !string.IsNullOrEmpty(orcamento.Aviso);
		}
		catch (ErroApi erro)
		{
			await DisplayAlert("Orçamento", erro.Message, "OK");
		}
	}

	private string NomeCategoria(Guid id) =>
		_categorias.FirstOrDefault(c => c.Id == id)?.Nome ?? "Categoria";

	private async void AoSelecionar(object? remetente, SelectionChangedEventArgs argumentos)
	{
		if (argumentos.CurrentSelection.FirstOrDefault() is not LinhaView linha)
		{
			return;
		}

		Lista.SelectedItem = null;
		await DefinirValorAsync(linha.CategoriaId, linha.Nome);
	}

	private async void AoAdicionar(object? remetente, EventArgs argumentos)
	{
		if (_tenant.SomenteLeitura)
		{
			await DisplayAlert("Somente leitura", "Este espaço é compartilhado só para consulta.", "OK");
			return;
		}

		if (_categorias.Count == 0)
		{
			await DisplayAlert("Orçamento", "Cadastre categorias primeiro (em Mais › Categorias e regras).", "OK");
			return;
		}

		var escolha = await DisplayActionSheet("Categoria", "Cancelar", null, [.. _categorias.Select(c => c.Nome)]);
		if (escolha is null or "Cancelar")
		{
			return;
		}

		var categoria = _categorias.First(c => c.Nome == escolha);
		await DefinirValorAsync(categoria.Id, categoria.Nome);
	}

	private async Task DefinirValorAsync(Guid categoriaId, string nome)
	{
		if (_tenant.SomenteLeitura)
		{
			return;
		}

		var texto = await DisplayPromptAsync("Planejar", $"Valor planejado para {nome}:", keyboard: Keyboard.Numeric);
		if (texto is null)
		{
			return;
		}

		if (!decimal.TryParse(texto.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var valor) || valor < 0)
		{
			await DisplayAlert("Orçamento", "Valor inválido.", "OK");
			return;
		}

		try
		{
			var resultado = await _api.DefinirOrcamentoAsync(new DateOnly(_mes.Year, _mes.Month, 1), categoriaId, valor);
			if (!string.IsNullOrEmpty(resultado.Aviso))
			{
				await DisplayAlert("Atenção", resultado.Aviso, "OK");
			}
			await CarregarAsync();
		}
		catch (ErroApi erro)
		{
			await DisplayAlert("Orçamento", erro.Message, "OK");
		}
	}

	private async void AoCopiar(object? remetente, EventArgs argumentos)
	{
		var destino = new DateOnly(_mes.Year, _mes.Month, 1);
		var origem = destino.AddMonths(-1);

		try
		{
			await _api.CopiarOrcamentoAsync(origem, destino, confirmado: false);
			await CarregarAsync();
		}
		catch (ErroApi erro) when (erro.Codigo == "confirmacao_necessaria")
		{
			// O destino já tem orçamento: confirma a sobrescrita antes de reenviar.
			var confirma = await DisplayAlert("Copiar orçamento", "Este mês já tem orçamento. Sobrescrever com o do mês anterior?", "Sobrescrever", "Cancelar");
			if (!confirma)
			{
				return;
			}

			try
			{
				await _api.CopiarOrcamentoAsync(origem, destino, confirmado: true);
				await CarregarAsync();
			}
			catch (ErroApi e)
			{
				await DisplayAlert("Orçamento", e.Message, "OK");
			}
		}
		catch (ErroApi erro)
		{
			await DisplayAlert("Orçamento", erro.Message, "OK");
		}
	}
}
