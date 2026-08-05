using System.Text.Json;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.Services.V1.Assinaturas;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Assinatura;

/// <summary>
/// Catálogo de planos, situação da conta, contratação e as notificações dos provedores.
/// </summary>
public sealed class Assinatura : IEndpoint
{
    public void Mapear(IEndpointRouteBuilder rotas)
    {
        // Vitrine: quem ainda não assinou precisa ver o que existe, e quem está suspenso precisa
        // ver para onde voltar. Exigir assinatura aqui trancaria a porta pelo lado de dentro.
        rotas.MapGet("/api/v1/planos", async (
                IPlanoReadRepository planos,
                CancellationToken cancellationToken) =>
            Results.Ok((await planos.ListarAtivosAsync(cancellationToken)).ParaResponse()))
            .RequireAuthorization()
            .WithName("CatalogoDePlanos")
            .WithTags("Assinatura");

        rotas.MapGet("/api/v1/assinatura", async (
                IAssinaturaService assinaturas,
                CancellationToken cancellationToken) =>
            Results.Ok((await assinaturas.SituacaoAtualAsync(cancellationToken)).ParaResponse()))
            .RequireAuthorization()
            .WithName("SituacaoDaAssinatura")
            .WithTags("Assinatura");

        rotas.MapPost("/api/v1/assinatura", async (
                ContratarRequest request,
                IAssinaturaService assinaturas,
                CancellationToken cancellationToken) =>
            {
                var resultado = await assinaturas.ContratarAsync(
                    request.PlanoCodigo, request.Canal, request.Comprovante, cancellationToken);

                return resultado.Sucesso
                    ? Results.Ok(new ContratacaoResponse(resultado.AssinaturaId!.Value))
                    : RespostaHttp.Falha([resultado.Erro!]);
            })
            .RequireAuthorization()
            .WithName("ContratarAssinatura")
            .WithTags("Assinatura");

        MapearNotificacoes(rotas);
    }

    /// <summary>
    /// Recebimento das notificações dos provedores.
    /// </summary>
    /// <remarks>
    /// Anônimo por natureza: é o servidor da loja falando com o nosso, e ele não tem token de
    /// usuário. Quem autentica o chamador é a assinatura da requisição do próprio provedor, e o
    /// que impede um terceiro de causar dano é que a rota não concede direito — ela consulta a
    /// referência e aplica o efeito de um evento que só o provedor sabe emitir corretamente.
    /// <para>
    /// Responde 200 mesmo para notificação repetida. Devolver erro faria o provedor reenviar em
    /// laço, e o efeito já foi aplicado na primeira vez.
    /// </para>
    /// </remarks>
    private static void MapearNotificacoes(IEndpointRouteBuilder rotas) =>
        rotas.MapPost("/api/v1/pagamentos/{canal}/notificacoes", async (
                string canal,
                NotificacaoRequest request,
                IAssinaturaService assinaturas,
                CancellationToken cancellationToken) =>
            {
                if (!Enum.TryParse<CanalPagamento>(canal, ignoreCase: true, out var canalPagamento))
                {
                    return Results.NotFound();
                }

                var resultado = await assinaturas.AplicarNotificacaoAsync(
                    canalPagamento,
                    request.Identificador,
                    request.Evento.ParaEvento(),
                    request.Referencia,
                    JsonSerializer.Serialize(request),
                    cancellationToken);

                return Results.Ok(new { processada = resultado.Processada });
            })
            .AllowAnonymous()
            .WithName("NotificacaoDePagamento")
            .WithTags("Assinatura");
}
