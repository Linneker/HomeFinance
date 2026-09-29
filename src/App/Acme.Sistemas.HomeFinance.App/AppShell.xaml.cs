using Acme.Sistemas.HomeFinance.App.Pages;
using Acme.Sistemas.HomeFinance.App.Services;

namespace Acme.Sistemas.HomeFinance.App;

public partial class AppShell : Shell
{
	private readonly ISessao _sessao;
	private readonly ContextoTenant _contextoTenant;
	private readonly INotificacaoDispositivo _push;

	public AppShell(ISessao sessao, ContextoTenant contextoTenant, INotificacaoDispositivo push)
	{
		InitializeComponent();
		_sessao = sessao;
		_contextoTenant = contextoTenant;
		_push = push;

		// Rotas navegáveis fora da árvore visível do Shell.
		Routing.RegisterRoute("criarconta", typeof(CriarContaPage));
		Routing.RegisterRoute("assinatura", typeof(AssinaturaPage));
		Routing.RegisterRoute("planos", typeof(PlanosPage));
		Routing.RegisterRoute("lancamento-novo", typeof(CriarLancamentoPage));
		Routing.RegisterRoute("renda", typeof(RendaPage));
		Routing.RegisterRoute("regras", typeof(RegrasPage));
		Routing.RegisterRoute("compartilhamento", typeof(CompartilhamentoPage));
		Routing.RegisterRoute("exportacao", typeof(ExportacaoPage));
		Routing.RegisterRoute("tenants", typeof(TenantsPage));
		Routing.RegisterRoute("resumo", typeof(ResumoPage));

		// Sessão que cai (logout ou expiração) devolve o app ao login, de qualquer tela, e limpa
		// o tenant ativo para não vazar a seleção anterior na próxima sessão.
		_sessao.Encerrada += (_, _) =>
			Dispatcher.Dispatch(async () =>
			{
				_contextoTenant.Limpar();
				await _push.RemoverAsync();
				await GoToAsync("//login");
			});

		Loaded += AoCarregar;
	}

	private async void AoCarregar(object? remetente, EventArgs argumentos)
	{
		Loaded -= AoCarregar;
		await _sessao.CarregarAsync();

		if (_sessao.Autenticado)
		{
			await GoToAsync("//main");
		}
	}
}
