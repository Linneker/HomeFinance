using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Assinatura;

/// <param name="Comprovante">
/// O que a loja ou o gateway devolveu. É validado no servidor antes de conceder qualquer
/// direito — nenhum campo daqui é aceito como verdade.
/// </param>
public sealed record ContratarRequest(
    string PlanoCodigo,
    CanalPagamento Canal,
    string Comprovante);

/// <param name="Identificador">
/// Identificador do evento no provedor. É a chave de idempotência: o provedor reenvia até
/// receber confirmação.
/// </param>
/// <param name="Referencia">Identificador da assinatura no provedor.</param>
public sealed record NotificacaoRequest(
    string Identificador,
    string Referencia,
    string Evento);
