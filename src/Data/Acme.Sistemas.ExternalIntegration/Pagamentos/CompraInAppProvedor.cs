using System.Net.Http.Json;
using System.Text.Json;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Infrastructure.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acme.Sistemas.ExternalIntegration.Pagamentos;

/// <summary>
/// Valida compra in-app junto à loja.
/// </summary>
/// <remarks>
/// As duas lojas compartilham este adaptador porque o que o sistema precisa delas é idêntico:
/// "este comprovante é válido e até quando?". O que muda é a URL e o formato do segredo, e isso
/// cabe em dois campos.
/// <para>
/// A credencial vem do OpenBao a cada chamada. Guardá-la em campo faria a rotação do segredo
/// exigir reinício da aplicação — e é justamente durante um incidente de credencial vazada que
/// ninguém quer depender de deploy.
/// </para>
/// </remarks>
public sealed class CompraInAppProvedor(
    HttpClient http,
    ISecretProvider segredos,
    IOptions<OpcoesDePagamento> opcoes,
    ILogger<CompraInAppProvedor> logger,
    CanalPagamento canal)
    : IProvedorDePagamento
{
    public CanalPagamento Canal { get; } = canal;

    private string Endereco => Canal == CanalPagamento.AppStore
        ? opcoes.Value.AppStoreUrl
        : opcoes.Value.GooglePlayUrl;

    private string ChaveDoSegredo => Canal == CanalPagamento.AppStore
        ? ChavesDeSegredo.AppStore
        : ChavesDeSegredo.GooglePlay;

    public Task<ConfirmacaoDeCompra> ConfirmarAsync(
        string comprovante,
        CancellationToken cancellationToken) =>
        PerguntarAsync(comprovante, cancellationToken);

    public Task<ConfirmacaoDeCompra> ConsultarAsync(
        string referencia,
        CancellationToken cancellationToken) =>
        PerguntarAsync(referencia, cancellationToken);

    private async Task<ConfirmacaoDeCompra> PerguntarAsync(
        string comprovante,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(comprovante))
        {
            return ConfirmacaoDeCompra.Recusada("comprovante_ausente");
        }

        var credencial = await segredos.GetAsync(ChaveDoSegredo, cancellationToken);

        try
        {
            using var resposta = await http.PostAsJsonAsync(
                Endereco,
                new { comprovante, credencial },
                cancellationToken);

            if (!resposta.IsSuccessStatusCode)
            {
                // Indisponibilidade não é recusa de compra: manter a assinatura como está é o
                // comportamento seguro dos dois lados — não concede direito e não tira o que já
                // havia por causa de uma falha nossa.
                logger.LogWarning(
                    "Loja {Canal} respondeu {Status} na validação.", Canal, resposta.StatusCode);

                return ConfirmacaoDeCompra.Recusada("provedor_indisponivel");
            }

            var corpo = await resposta.Content.ReadFromJsonAsync<RespostaDaLoja>(
                cancellationToken: cancellationToken);

            return Interpretar(corpo);
        }
        catch (Exception excecao) when (excecao is HttpRequestException or JsonException
                                            or TaskCanceledException)
        {
            logger.LogWarning(excecao, "Falha ao falar com a loja {Canal}.", Canal);

            return ConfirmacaoDeCompra.Recusada("provedor_indisponivel");
        }
    }

    private static ConfirmacaoDeCompra Interpretar(RespostaDaLoja? corpo) =>
        corpo is { Valido: true, Referencia.Length: > 0, ExpiraEm: not null }
            ? ConfirmacaoDeCompra.Aceita(corpo.Referencia, corpo.ExpiraEm.Value)
            : ConfirmacaoDeCompra.Recusada(corpo?.Motivo ?? "comprovante_nao_confirmado");

    private sealed record RespostaDaLoja(
        bool Valido,
        string Referencia,
        DateTimeOffset? ExpiraEm,
        string? Motivo);
}
