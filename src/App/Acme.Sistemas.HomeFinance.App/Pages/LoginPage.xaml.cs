using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App.Pages;

public partial class LoginPage : ContentPage
{
	private readonly IdentidadeApi _identidade;

	public LoginPage(IdentidadeApi identidade)
	{
		InitializeComponent();
		_identidade = identidade;
	}

	private async void AoEntrar(object? remetente, EventArgs argumentos)
	{
		LabelErro.IsVisible = false;

		if (string.IsNullOrWhiteSpace(EntradaEmail.Text) || string.IsNullOrWhiteSpace(EntradaSenha.Text))
		{
			Mostrar("Informe e-mail e senha.");
			return;
		}

		Ocupar(true);
		try
		{
			await _identidade.AutenticarAsync(EntradaEmail.Text.Trim(), EntradaSenha.Text);
			EntradaSenha.Text = string.Empty;
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

	private async void AoCriarConta(object? remetente, EventArgs argumentos) =>
		await Shell.Current.GoToAsync("criarconta");

	private void Mostrar(string mensagem)
	{
		LabelErro.Text = mensagem;
		LabelErro.IsVisible = true;
	}

	private void Ocupar(bool ocupado)
	{
		Ocupado.IsRunning = ocupado;
		Ocupado.IsVisible = ocupado;
		BotaoEntrar.IsEnabled = !ocupado;
	}
}
