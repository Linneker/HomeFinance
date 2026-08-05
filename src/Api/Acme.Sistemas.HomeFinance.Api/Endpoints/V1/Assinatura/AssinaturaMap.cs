using Acme.Sistemas.Domain.Entities.Assinaturas;
using Acme.Sistemas.Services.V1.Assinaturas;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Assinatura;

internal static class AssinaturaMap
{
    public static CatalogoResponse ParaResponse(this IReadOnlyList<PlanoEntity> planos) =>
        new([.. planos.Select(plano => new PlanoResponse(
            plano.Codigo, plano.Nome, plano.CotaCompartilhamentos, plano.PrecoMensal))]);

    public static SituacaoResponse ParaResponse(this SituacaoDaConta situacao) =>
        new(
            situacao.Situacao,
            situacao.Vigente,
            situacao.PlanoCodigo,
            situacao.CotaTotal,
            situacao.CotaUsada,
            situacao.CotaDisponivel);

    /// <summary>
    /// Traduz o rótulo do provedor para o evento do domínio.
    /// </summary>
    /// <remarks>
    /// Desconhecido vira estorno de propósito: entre suspender indevidamente e liberar de graça
    /// um evento que não sabemos ler, o primeiro é recuperável por uma reconciliação e o segundo
    /// não aparece em lugar nenhum.
    /// </remarks>
    public static EventoDoProvedor ParaEvento(this string rotulo) => rotulo.ToLowerInvariant() switch
    {
        "renewal" or "renovacao" or "payment.approved" or "PAYMENT_CONFIRMED" => EventoDoProvedor.Renovacao,
        "cancel" or "cancelamento" or "subscription.cancelled" => EventoDoProvedor.CancelamentoAgendado,
        _ => EventoDoProvedor.Estorno,
    };
}
