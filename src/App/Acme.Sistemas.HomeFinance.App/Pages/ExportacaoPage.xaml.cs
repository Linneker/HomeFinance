using Acme.Sistemas.HomeFinance.App.Helpers;
using Acme.Sistemas.HomeFinance.App.Models;
using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App.Pages;

public partial class ExportacaoPage : ContentPage
{
	private readonly FinancasApi _api;
	private Guid? _solicitacaoId;

	private static readonly (string Rotulo, FormatoExportacao Formato)[] Formatos =
	[
		("CSV", FormatoExportacao.Csv),
		("JSON", FormatoExportacao.Json),
		("Texto", FormatoExportacao.Texto),
	];

	public ExportacaoPage(FinancasApi api)
	{
		InitializeComponent();
		_api = api;
		SeletorFormato.ItemsSource = Formatos.Select(f => f.Rotulo).ToList();
		SeletorFormato.SelectedIndex = 0;
	}

	private async void AoSolicitar(object? remetente, EventArgs argumentos)
	{
		LabelErro.IsVisible = false;
		var formato = Formatos[Math.Max(0, SeletorFormato.SelectedIndex)].Formato;

		Ocupar(true);
		try
		{
			var solicitada = await _api.SolicitarExportacaoAsync(formato);
			_solicitacaoId = solicitada.SolicitacaoId;
			PainelSituacao.IsVisible = true;
			await AtualizarSituacaoAsync();
		}
		catch (ErroApi erro) when (erro.Codigo == "exportacao_nao_habilitada")
		{
			// Não habilitada: mostra o estado indisponível com o motivo, como a spec pede.
			PainelSolicitar.IsVisible = false;
			PainelIndisponivel.IsVisible = true;
		}
		catch (ErroApi erro)
		{
			Mostrar(erro.Message);
		}
		finally
		{
			Ocupar(false);
		}
	}

	private async void AoAtualizarSituacao(object? remetente, EventArgs argumentos) => await AtualizarSituacaoAsync();

	private async Task AtualizarSituacaoAsync()
	{
		if (_solicitacaoId is not { } id)
		{
			return;
		}

		try
		{
			var s = await _api.SituacaoExportacaoAsync(id);
			LabelSituacao.Text = $"Situação: {Formato.Exportacao(s.Situacao)}";
			LabelExpira.Text = s.ExpiraEm is { } expira ? $"Disponível até {Formato.DataHora(expira)}" : string.Empty;
			BotaoBaixar.IsVisible = s.DisponivelParaDownload;
		}
		catch (ErroApi erro)
		{
			Mostrar(erro.Message);
		}
	}

	private async void AoBaixar(object? remetente, EventArgs argumentos)
	{
		if (_solicitacaoId is not { } id)
		{
			return;
		}

		Ocupar(true);
		try
		{
			var (conteudo, nome) = await _api.BaixarExportacaoAsync(id, "homefinance-exportacao");
			var caminho = Path.Combine(FileSystem.CacheDirectory, nome);
			await File.WriteAllBytesAsync(caminho, conteudo);

			await Share.Default.RequestAsync(new ShareFileRequest
			{
				Title = "Exportação HomeFinance",
				File = new ShareFile(caminho),
			});
		}
		catch (ErroApi erro)
		{
			Mostrar(erro.Message);
		}
		catch
		{
			Mostrar("Não foi possível baixar o arquivo.");
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
		BotaoSolicitar.IsEnabled = !ocupado;
	}
}
