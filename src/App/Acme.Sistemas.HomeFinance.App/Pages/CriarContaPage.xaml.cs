using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App.Pages;

public partial class CriarContaPage : ContentPage
{
	private readonly IdentidadeApi _identidade;

	public CriarContaPage(IdentidadeApi identidade)
	{
		InitializeComponent();
		_identidade = identidade;
	}

	private async void AoCriar(object? remetente, EventArgs argumentos)
	{
		LabelErro.IsVisible = false;

		var email = EntradaEmail.Text?.Trim() ?? string.Empty;
		var senha = EntradaSenha.Text ?? string.Empty;

		if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
		{
			Mostrar("Informe e-mail e senha.");
			return;
		}

		if (senha != EntradaConfirmar.Text)
		{
			Mostrar("As senhas não conferem.");
			return;
		}

		// O aceite é obrigatório e registrado na criação: sem ele, não há conta.
		if (!AceiteTermos.IsChecked)
		{
			Mostrar("É preciso aceitar os termos para criar a conta.");
			return;
		}

		Ocupar(true);
		try
		{
			await _identidade.CriarContaAsync(email, senha, aceitouTermos: true);
			await Shell.Current.GoToAsync("//main");
		}
		catch (ErroApi erro)
		{
			Mostrar(erro.Message);
		}
		catch
		{
			Mostrar("Não foi possível conectar. Tente novamente.");
		}
		finally
		{
			Ocupar(false);
		}
	}

	private void Mostrar(string mensagem)
	{
		LabelErro.Text = mensagem;
		LabelErro.IsVisible = true;
	}

	private void Ocupar(bool ocupado)
	{
		Ocupado.IsRunning = ocupado;
		Ocupado.IsVisible = ocupado;
		BotaoCriar.IsEnabled = !ocupado;
	}
}
