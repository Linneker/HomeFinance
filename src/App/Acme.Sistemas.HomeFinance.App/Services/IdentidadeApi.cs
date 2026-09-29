using Acme.Sistemas.HomeFinance.App.Models;

namespace Acme.Sistemas.HomeFinance.App.Services;

/// <summary>
/// Fluxos de identidade: criar conta com aceite de termos, autenticar e registrar consentimento.
/// </summary>
public sealed class IdentidadeApi(ApiClient api, ISessao sessao)
{
    /// <summary>Versão dos documentos que o app exibe e faz aceitar. Sobe quando os termos mudam.</summary>
    public const string VersaoDocumentos = "1.0";

    public async Task AutenticarAsync(string email, string senha, CancellationToken ct = default)
    {
        var sessaoResposta = await api.PostAsync<SessaoResposta>(
            "/api/v1/sessoes", new { email, senha }, ct);

        await sessao.DefinirAsync(sessaoResposta.Token, sessaoResposta.ExpiraEm);
    }

    /// <summary>
    /// Cria a conta e já autentica em seguida, para o onboarding continuar sem uma segunda tela
    /// de login. O aceite dos termos é obrigatório e vai no mesmo passo.
    /// </summary>
    public async Task<CriarContaResposta> CriarContaAsync(
        string email,
        string senha,
        bool aceitouTermos,
        CancellationToken ct = default)
    {
        var conta = await api.PostAsync<CriarContaResposta>(
            "/api/v1/contas",
            new { email, senha, aceitouTermos, versaoDocumentos = VersaoDocumentos },
            ct);

        await AutenticarAsync(email, senha, ct);
        return conta;
    }

    public Task RegistrarConsentimentoAsync(string documento, CancellationToken ct = default) =>
        api.PostAsync("/api/v1/consentimentos", new { documento, versao = VersaoDocumentos }, ct);

    public Task<ConsentimentoVigencia> VigenciaConsentimentoAsync(CancellationToken ct = default) =>
        api.GetAsync<ConsentimentoVigencia>(
            $"/api/v1/consentimentos/vigencia?versaoAtual={VersaoDocumentos}", ct);
}
