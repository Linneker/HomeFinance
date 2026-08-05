using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Infrastructure.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acme.Sistemas.ExternalIntegration.Pagamentos;

/// <summary>
/// Canais secundários: Mercado Pago e Asaas.
/// </summary>
/// <remarks>
/// Os dois expõem a mesma forma para o que interessa aqui — consultar uma assinatura por
/// identificador e saber se está paga e até quando. Um adaptador por provedor com o mesmo corpo
/// duplicaria a lógica de erro e de timeout em dois lugares que teriam de ser corrigidos juntos.
/// <para>
/// Nenhum dado de cartão passa por aqui, nem de ida nem de volta: o pagamento acontece no
/// provedor e o que trafega é identificador. Identificador vazado não compra nada.
/// </para>
/// </remarks>
public sealed class GatewayDePagamento(
    HttpClient http,
    ISecretProvider segredos,
    IOptions<OpcoesDePagamento> opcoes,
    ILogger<GatewayDePagamento> logger,
    CanalPagamento canal)
    : IProvedorDePagamento
{
    public CanalPagamento Canal { get; } = canal;

    private string Endereco => Canal == CanalPagamento.MercadoPago
        ? opcoes.Value.MercadoPagoUrl
        : opcoes.Value.AsaasUrl;

    private string ChaveDoSegredo => Canal == CanalPagamento.MercadoPago
        ? ChavesDeSegredo.MercadoPago
        : ChavesDeSegredo.Asaas;

    /// <summary>
    /// No canal secundário o "comprovante" já é o identificador da assinatura no provedor: o
    /// pagamento aconteceu lá, e o que resta é confirmar que está pago.
    /// </summary>
    public Task<ConfirmacaoDeCompra> ConfirmarAsync(
        string comprovante,
        CancellationToken cancellationToken) =>
        ConsultarAsync(comprovante, cancellationToken);

    public async Task<ConfirmacaoDeCompra> ConsultarAsync(
        string referencia,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(referencia))
        {
            return ConfirmacaoDeCompra.Recusada("referencia_ausente");
        }

        var credencial = await segredos.GetAsync(ChaveDoSegredo, cancellationToken);

        try
        {
            using var requisicao = new HttpRequestMessage(
                HttpMethod.Get,
                $"{Endereco.TrimEnd('/')}/subscriptions/{Uri.EscapeDataString(referencia)}");

            AutenticarNo(requisicao, credencial);

            using var resposta = await http.SendAsync(requisicao, cancellationToken);

            if (!resposta.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Provedor {Canal} respondeu {Status} na consulta.", Canal, resposta.StatusCode);

                return ConfirmacaoDeCompra.Recusada("provedor_indisponivel");
            }

            var corpo = await resposta.Content.ReadFromJsonAsync<RespostaDoGateway>(
                cancellationToken: cancellationToken);

            return corpo is { Status: "authorized" or "active" or "ACTIVE", ProximoVencimento: not null }
                ? ConfirmacaoDeCompra.Aceita(referencia, corpo.ProximoVencimento.Value)
                : ConfirmacaoDeCompra.Recusada(corpo?.Status ?? "assinatura_nao_confirmada");
        }
        catch (Exception excecao) when (excecao is HttpRequestException or JsonException
                                            or TaskCanceledException)
        {
            logger.LogWarning(excecao, "Falha ao falar com o provedor {Canal}.", Canal);

            return ConfirmacaoDeCompra.Recusada("provedor_indisponivel");
        }
    }

    /// <remarks>
    /// Mercado Pago usa Bearer; Asaas usa um cabeçalho próprio. É a única diferença de protocolo
    /// que sobrou depois de a forma da resposta ser normalizada.
    /// </remarks>
    private void AutenticarNo(HttpRequestMessage requisicao, string credencial)
    {
        if (Canal == CanalPagamento.MercadoPago)
        {
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credencial);
            return;
        }

        requisicao.Headers.Add("access_token", credencial);
    }

    private sealed record RespostaDoGateway(string? Status, DateTimeOffset? ProximoVencimento);
}
