using System.Globalization;
using Acme.Sistemas.HomeFinance.App.Models;

namespace Acme.Sistemas.HomeFinance.App.Services;

/// <summary>
/// Acesso aos endpoints de dado do usuário final. Agrupado num serviço só porque todos partem do
/// mesmo <see cref="ApiClient"/> e do mesmo contexto de tenant; a organização por feature fica
/// nos métodos.
/// </summary>
public sealed class FinancasApi(ApiClient api)
{
    private static string Dia(DateOnly data) => data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // ---- Assinatura ----
    public Task<Catalogo> PlanosAsync(CancellationToken ct = default) =>
        api.GetAsync<Catalogo>("/api/v1/planos", ct);

    public Task<SituacaoAssinaturaResposta> SituacaoAssinaturaAsync(CancellationToken ct = default) =>
        api.GetAsync<SituacaoAssinaturaResposta>("/api/v1/assinatura", ct);

    public Task<Contratacao> ContratarAsync(string planoCodigo, CanalPagamento canal, string comprovante, CancellationToken ct = default) =>
        api.PostAsync<Contratacao>("/api/v1/assinatura", new { planoCodigo, canal, comprovante }, ct);

    // ---- Onboarding ----
    public Task<ProgressoOnboarding> OnboardingAsync(CancellationToken ct = default) =>
        api.GetAsync<ProgressoOnboarding>("/api/v1/onboarding", ct);

    // ---- Tenants ----
    public Task<TenantsDisponiveis> TenantsAsync(CancellationToken ct = default) =>
        api.GetAsync<TenantsDisponiveis>("/api/v1/tenants", ct);

    // ---- Categorias ----
    public Task<CategoriasResposta> CategoriasAsync(CancellationToken ct = default) =>
        api.GetAsync<CategoriasResposta>("/api/v1/categorias", ct);

    public Task<CategoriaCriada> CriarCategoriaAsync(string nome, CancellationToken ct = default) =>
        api.PostAsync<CategoriaCriada>("/api/v1/categorias", new { nome }, ct);

    // ---- Lançamentos ----
    public Task<LancamentosResposta> LancamentosAsync(DateOnly de, DateOnly ate, bool porCaixa, CancellationToken ct = default) =>
        api.GetAsync<LancamentosResposta>($"/api/v1/lancamentos?de={Dia(de)}&ate={Dia(ate)}&porCaixa={porCaixa.ToString().ToLowerInvariant()}", ct);

    /// <summary>
    /// Cria um lançamento. Devolve <c>Conflito</c> preenchido quando a Api detecta possível
    /// duplicidade (409): a UI confirma e reenvia com <c>confirmouDuplicidade = true</c>.
    /// </summary>
    public Task<(LancamentoCriado? Ok, PossivelDuplicidade? Conflito)> CriarLancamentoAsync(object corpo, CancellationToken ct = default) =>
        api.PostOuConflitoAsync<LancamentoCriado, PossivelDuplicidade>("/api/v1/lancamentos", corpo, ct);

    public Task<RecorrenciaCriada> CriarRecorrenciaAsync(object corpo, CancellationToken ct = default) =>
        api.PostAsync<RecorrenciaCriada>("/api/v1/recorrencias", corpo, ct);

    public Task<LancamentoAlterado> AlterarLancamentoAsync(Guid id, object corpo, CancellationToken ct = default) =>
        api.PutAsync<LancamentoAlterado>($"/api/v1/lancamentos/{id}", corpo, ct);

    public Task ExcluirLancamentoAsync(Guid id, CancellationToken ct = default) =>
        api.DeleteAsync($"/api/v1/lancamentos/{id}", ct);

    public Task<ClassificacaoAplicada> ClassificarAsync(Guid lancamentoId, Guid categoriaId, bool criarRegra, CancellationToken ct = default) =>
        api.PutAsync<ClassificacaoAplicada>($"/api/v1/lancamentos/{lancamentoId}/classificacao", new { categoriaId, criarRegra }, ct);

    // ---- Renda ----
    public Task<FontesRenda> RendasAsync(CancellationToken ct = default) =>
        api.GetAsync<FontesRenda>("/api/v1/rendas", ct);

    public Task<FonteRendaCadastrada> CadastrarRendaAsync(object corpo, CancellationToken ct = default) =>
        api.PostAsync<FonteRendaCadastrada>("/api/v1/rendas", corpo, ct);

    // ---- Orçamento ----
    public Task<OrcamentoDoMes> OrcamentoAsync(DateOnly competencia, CancellationToken ct = default) =>
        api.GetAsync<OrcamentoDoMes>($"/api/v1/orcamentos/{Dia(competencia)}", ct);

    public Task<OrcamentoDefinido> DefinirOrcamentoAsync(DateOnly competencia, Guid categoriaId, decimal valorPlanejado, CancellationToken ct = default) =>
        api.PutAsync<OrcamentoDefinido>($"/api/v1/orcamentos/{Dia(competencia)}", new { categoriaId, valorPlanejado }, ct);

    public Task<OrcamentoCopiado> CopiarOrcamentoAsync(DateOnly origem, DateOnly destino, bool confirmado, CancellationToken ct = default) =>
        api.PostAsync<OrcamentoCopiado>("/api/v1/orcamentos/copias", new { origem = Dia(origem), destino = Dia(destino), confirmado }, ct);

    // ---- Relatório ----
    public Task<Resumo> ResumoAsync(DateOnly competencia, bool porCaixa, CancellationToken ct = default) =>
        api.GetAsync<Resumo>($"/api/v1/relatorios/{Dia(competencia)}/resumo?porCaixa={porCaixa.ToString().ToLowerInvariant()}", ct);

    // ---- Alertas ----
    public Task<AlertasResposta> AlertasAsync(CancellationToken ct = default) =>
        api.GetAsync<AlertasResposta>("/api/v1/alertas", ct);

    public Task DispensarAlertaAsync(Guid id, CancellationToken ct = default) =>
        api.PostAsync($"/api/v1/alertas/{id}/dispensa", null, ct);

    // ---- Regras ----
    public Task<RegrasResposta> RegrasAsync(CancellationToken ct = default) =>
        api.GetAsync<RegrasResposta>("/api/v1/regras-classificacao", ct);

    public Task ExcluirRegraAsync(Guid regraId, CancellationToken ct = default) =>
        api.DeleteAsync($"/api/v1/regras-classificacao/{regraId}", ct);

    // ---- Compartilhamento ----
    public Task<ItensConsentimento> ItensConsentimentoAsync(CancellationToken ct = default) =>
        api.GetAsync<ItensConsentimento>("/api/v1/compartilhamentos/consentimento", ct);

    public Task<IReadOnlyList<CompartilhamentoConcedido>> CompartilhamentosAsync(CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<CompartilhamentoConcedido>>("/api/v1/compartilhamentos", ct);

    public Task<CompartilhamentoCriado> CompartilharAsync(string emailDestinatario, bool consentimentoConfirmado, bool incluiScr, CancellationToken ct = default) =>
        api.PostAsync<CompartilhamentoCriado>("/api/v1/compartilhamentos", new { emailDestinatario, consentimentoConfirmado, incluiScr }, ct);

    public Task RevogarCompartilhamentoAsync(Guid destinatarioId, CancellationToken ct = default) =>
        api.DeleteAsync($"/api/v1/compartilhamentos/{destinatarioId}", ct);

    // ---- Exportação ----
    public Task<ExportacaoSolicitada> SolicitarExportacaoAsync(FormatoExportacao formato, CancellationToken ct = default) =>
        api.PostAsync<ExportacaoSolicitada>("/api/v1/exportacoes", new { formato }, ct);

    public Task<SituacaoExportacaoResposta> SituacaoExportacaoAsync(Guid id, CancellationToken ct = default) =>
        api.GetAsync<SituacaoExportacaoResposta>($"/api/v1/exportacoes/{id}", ct);

    public Task<(byte[] Conteudo, string Nome)> BaixarExportacaoAsync(Guid id, string nomePadrao, CancellationToken ct = default) =>
        api.GetArquivoAsync($"/api/v1/exportacoes/{id}/arquivo", nomePadrao, ct);

    // ---- Conta ----
    public Task<ContaExcluida> ExcluirContaAsync(CancellationToken ct = default) =>
        api.DeleteAsync<ContaExcluida>("/api/v1/contas/minha?confirmacao=true", ct);
}
