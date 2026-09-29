using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App.Pages;

public partial class MaisPage : ContentPage
{
	private readonly FinancasApi _api;
	private readonly ISessao _sessao;

	public MaisPage(FinancasApi api, ISessao sessao)
	{
		InitializeComponent();
		_api = api;
		_sessao = sessao;
	}

	private async void AoResumo(object? s, EventArgs e) => await Shell.Current.GoToAsync("resumo");
	private async void AoAssinatura(object? s, EventArgs e) => await Shell.Current.GoToAsync("assinatura");
	private async void AoRenda(object? s, EventArgs e) => await Shell.Current.GoToAsync("renda");
	private async void AoRegras(object? s, EventArgs e) => await Shell.Current.GoToAsync("regras");
	private async void AoCompartilhamento(object? s, EventArgs e) => await Shell.Current.GoToAsync("compartilhamento");
	private async void AoExportacao(object? s, EventArgs e) => await Shell.Current.GoToAsync("exportacao");
	private async void AoTenants(object? s, EventArgs e) => await Shell.Current.GoToAsync("tenants");

	private async void AoSair(object? s, EventArgs e) => await _sessao.LimparAsync();

	private async void AoExcluir(object? remetente, EventArgs argumentos)
	{
		var confirma = await DisplayAlert(
			"Excluir conta",
			"Isto remove sua conta e seus dados. A ação é irreversível. Deseja continuar?",
			"Excluir",
			"Cancelar");

		if (!confirma)
		{
			return;
		}

		try
		{
			var resultado = await _api.ExcluirContaAsync();
			if (resultado.Excluida)
			{
				await _sessao.LimparAsync();
			}
		}
		catch (ErroApi erro)
		{
			await DisplayAlert("Excluir conta", erro.Message, "OK");
		}
		catch
		{
			await DisplayAlert("Excluir conta", "Não foi possível excluir a conta agora.", "OK");
		}
	}
}
