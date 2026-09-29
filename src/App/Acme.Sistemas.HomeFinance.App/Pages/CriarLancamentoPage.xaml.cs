using System.Globalization;
using Acme.Sistemas.HomeFinance.App.Models;
using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App.Pages;

public partial class CriarLancamentoPage : ContentPage
{
	private readonly FinancasApi _api;
	private List<CategoriaItem> _categorias = [];

	public CriarLancamentoPage(FinancasApi api)
	{
		InitializeComponent();
		_api = api;
		DataCompetencia.Date = DateTime.Today;
		DataCaixa.Date = DateTime.Today;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		try
		{
			_categorias = [.. (await _api.CategoriasAsync()).Categorias];
			SeletorCategoria.ItemsSource = new List<string> { "(sem categoria)" }
				.Concat(_categorias.Select(c => c.Nome))
				.ToList();
			SeletorCategoria.SelectedIndex = 0;
		}
		catch
		{
			// Sem categorias, o lançamento pode nascer pendente de classificação.
		}
	}

	private void AoAlternarRecorrente(object? s, ToggledEventArgs e)
	{
		// Recorrência é mensal e não parcela; a data de caixa é derivada por ocorrência.
		PainelParcelas.IsVisible = !SwitchRecorrente.IsToggled;
		DataCaixa.IsEnabled = !SwitchRecorrente.IsToggled;
	}

	private void AoMudarParcelas(object? s, ValueChangedEventArgs e) =>
		LabelParcelas.Text = $"{(int)Parcelas.Value}x";

	private Guid? CategoriaSelecionada()
	{
		var indice = SeletorCategoria.SelectedIndex;
		return indice <= 0 ? null : _categorias[indice - 1].Id;
	}

	private async void AoSalvar(object? remetente, EventArgs argumentos)
	{
		LabelErro.IsVisible = false;

		var descricao = EntradaDescricao.Text?.Trim() ?? string.Empty;
		if (descricao.Length == 0)
		{
			Mostrar("Informe a descrição.");
			return;
		}

		if (!decimal.TryParse((EntradaValor.Text ?? string.Empty).Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var valor) || valor <= 0)
		{
			Mostrar("Informe um valor válido.");
			return;
		}

		var competencia = DateOnly.FromDateTime(DataCompetencia.Date ?? DateTime.Today);
		var categoriaId = CategoriaSelecionada();

		Ocupar(true);
		try
		{
			if (SwitchRecorrente.IsToggled)
			{
				await _api.CriarRecorrenciaAsync(new
				{
					descricao,
					valor,
					diaDoMes = competencia.Day,
					categoriaId,
					inicio = competencia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				});
				await Sair();
				return;
			}

			await CriarLancamentoAsync(descricao, valor, competencia, categoriaId, confirmou: false);
		}
		catch (ErroApi erro)
		{
			Mostrar(erro.Message);
		}
		catch
		{
			Mostrar("Não foi possível salvar.");
		}
		finally
		{
			Ocupar(false);
		}
	}

	private async Task CriarLancamentoAsync(string descricao, decimal valor, DateOnly competencia, Guid? categoriaId, bool confirmou)
	{
		var corpo = new
		{
			descricao,
			valor,
			dataCompetencia = competencia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
			dataCaixa = DateOnly.FromDateTime(DataCaixa.Date ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
			categoriaId,
			parcelas = (int)Parcelas.Value,
			confirmouDuplicidade = confirmou,
		};

		var (ok, conflito) = await _api.CriarLancamentoAsync(corpo);

		if (ok is not null)
		{
			await Sair();
			return;
		}

		if (conflito is not null)
		{
			// Possível duplicidade: confirma e reenvia marcando a confirmação.
			var seguir = await DisplayAlert("Possível duplicidade", conflito.Mensagem, "Salvar assim mesmo", "Cancelar");
			if (seguir)
			{
				await CriarLancamentoAsync(descricao, valor, competencia, categoriaId, confirmou: true);
			}
		}
	}

	private async Task Sair() => await Shell.Current.GoToAsync("..");

	private void Mostrar(string mensagem)
	{
		LabelErro.Text = mensagem;
		LabelErro.IsVisible = true;
	}

	private void Ocupar(bool ocupado)
	{
		Ocupado.IsRunning = ocupado;
		Ocupado.IsVisible = ocupado;
		BotaoSalvar.IsEnabled = !ocupado;
	}
}
