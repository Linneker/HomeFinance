using Acme.Sistemas.HomeFinance.App.Helpers;
using Acme.Sistemas.HomeFinance.App.Models;
using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App.Pages;

public partial class AssinaturaPage : ContentPage
{
	private readonly FinancasApi _api;

	public AssinaturaPage(FinancasApi api)
	{
		InitializeComponent();
		_api = api;
	}

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
		LabelErro.IsVisible = false;
		try
		{
			var s = await _api.SituacaoAssinaturaAsync();
			LabelSituacao.Text = Formato.Assinatura(s.Situacao);
			LabelPlano.Text = $"Plano: {s.PlanoCodigo}";
			LabelCota.Text = $"Compartilhamentos: {s.CotaUsada} de {s.CotaTotal} usados ({s.CotaDisponivel} disponíveis).";

			// Não vigente ou em carência: mostra o caminho de regularização com o motivo claro.
			var precisaRegularizar = s.Situacao is not SituacaoAssinatura.Ativa;
			PainelRegularizar.IsVisible = precisaRegularizar;
			LabelRegularizar.Text = s.Situacao switch
			{
				SituacaoAssinatura.Carencia =>
					"Sua assinatura está em carência: o acesso segue liberado por alguns dias. Regularize o pagamento para não ser suspensa.",
				SituacaoAssinatura.Suspensa =>
					"Sua assinatura está suspensa. Registrar lançamentos e consultar seguem disponíveis, mas os recursos derivados ficam indisponíveis até a regularização.",
				SituacaoAssinatura.Cancelada =>
					"Sua assinatura foi cancelada. Contrate um plano para voltar a ter os recursos.",
				_ => string.Empty,
			};
		}
		catch (ErroApi erro)
		{
			Mostrar(erro.Message);
		}
		catch
		{
			Mostrar("Não foi possível carregar a assinatura.");
		}
	}

	private async void AoVerPlanos(object? remetente, EventArgs argumentos) =>
		await Shell.Current.GoToAsync("planos");

	private async void AoRegularizar(object? remetente, EventArgs argumentos) =>
		await Shell.Current.GoToAsync("planos");

	private void Mostrar(string mensagem)
	{
		LabelErro.Text = mensagem;
		LabelErro.IsVisible = true;
	}
}
