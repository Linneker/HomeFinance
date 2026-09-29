using Acme.Sistemas.HomeFinance.App.Helpers;
using Acme.Sistemas.HomeFinance.App.Models;
using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App.Pages;

public partial class CompartilhamentoPage : ContentPage
{
	private readonly FinancasApi _api;
	private readonly List<(ItemConsentimento Item, Switch Controle)> _itens = [];

	public CompartilhamentoPage(FinancasApi api)
	{
		InitializeComponent();
		_api = api;
	}

	private sealed record ShareView(Guid DestinatarioId, string Titulo, string Subtitulo);

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await CarregarAsync();
	}

	private async Task CarregarAsync()
	{
		LabelErro.IsVisible = false;
		await Task.WhenAll(CarregarCotaAsync(), CarregarListaAsync(), CarregarItensAsync());
	}

	private async Task CarregarCotaAsync()
	{
		try
		{
			var s = await _api.SituacaoAssinaturaAsync();
			LabelCota.Text = $"Cota do plano: {s.CotaUsada} de {s.CotaTotal} usados ({s.CotaDisponivel} disponíveis).";
		}
		catch
		{
			LabelCota.Text = string.Empty;
		}
	}

	private async Task CarregarListaAsync()
	{
		try
		{
			var shares = await _api.CompartilhamentosAsync();
			Lista.ItemsSource = shares
				.Select(c => new ShareView(
					c.DestinatarioId,
					c.IncluiScr ? "Acesso (inclui SCR)" : "Acesso somente leitura",
					$"Desde {Formato.DataHora(c.ConcedidoEm)}"))
				.ToList();
		}
		catch
		{
			Lista.ItemsSource = Array.Empty<ShareView>();
		}
	}

	private async Task CarregarItensAsync()
	{
		if (_itens.Count > 0)
		{
			return;
		}

		try
		{
			var itens = await _api.ItensConsentimentoAsync();
			foreach (var item in itens.Itens)
			{
				var linha = new VerticalStackLayout { Spacing = 0 };
				var topo = new HorizontalStackLayout { Spacing = 8 };
				// Sensível entra desmarcado por padrão — incluir dado sensível exige ação deliberada.
				var controle = new Switch { IsToggled = item.MarcadoPorPadrao };
				var titulo = new Label
				{
					Text = item.Sensivel ? $"{item.Titulo} (sensível)" : item.Titulo,
					VerticalOptions = LayoutOptions.Center,
					FontAttributes = FontAttributes.Bold,
				};
				topo.Add(controle);
				topo.Add(titulo);
				linha.Add(topo);
				linha.Add(new Label { Text = item.Descricao, FontSize = 12, TextColor = Colors.Gray });
				PainelItens.Add(linha);
				_itens.Add((item, controle));
			}
		}
		catch
		{
			// Sem os itens, o compartilhamento não é oferecido: confirmar sem ver o que é seria vazio.
			BotaoCompartilhar.IsEnabled = false;
		}
	}

	private async void AoCompartilhar(object? remetente, EventArgs argumentos)
	{
		LabelErro.IsVisible = false;

		var email = EntradaEmail.Text?.Trim() ?? string.Empty;
		if (email.Length == 0)
		{
			Mostrar("Informe o e-mail de quem vai receber.");
			return;
		}

		if (!Confirmo.IsChecked)
		{
			Mostrar("Confirme o consentimento para compartilhar.");
			return;
		}

		// O único dado sensível concedível pelo contrato é o SCR: seu estado vira incluiScr.
		var incluiScr = _itens.Any(x => x.Item.Sensivel && x.Controle.IsToggled);

		try
		{
			await _api.CompartilharAsync(email, consentimentoConfirmado: true, incluiScr);
			EntradaEmail.Text = string.Empty;
			Confirmo.IsChecked = false;
			await DisplayAlert("Compartilhamento", "Acesso concedido.", "OK");
			await CarregarAsync();
		}
		catch (ErroApi erro)
		{
			Mostrar(erro.Message);
		}
	}

	private async void AoRevogar(object? remetente, EventArgs argumentos)
	{
		if (remetente is not Button { CommandParameter: Guid destinatarioId })
		{
			return;
		}

		var confirma = await DisplayAlert("Revogar", "Encerrar o acesso desta pessoa ao seu espaço?", "Revogar", "Cancelar");
		if (!confirma)
		{
			return;
		}

		try
		{
			await _api.RevogarCompartilhamentoAsync(destinatarioId);
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
