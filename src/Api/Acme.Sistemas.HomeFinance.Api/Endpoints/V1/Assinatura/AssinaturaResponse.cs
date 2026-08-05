using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Assinatura;

public sealed record PlanoResponse(
    string Codigo,
    string Nome,
    int CotaCompartilhamentos,
    decimal PrecoMensal);

public sealed record CatalogoResponse(IReadOnlyList<PlanoResponse> Planos);

/// <param name="Vigente">
/// Inclui a carência. É esta a pergunta que o app deve fazer para habilitar recurso derivado —
/// comparar com "ativa" cortaria quem o produto decidiu tolerar.
/// </param>
public sealed record SituacaoResponse(
    SituacaoAssinatura Situacao,
    bool Vigente,
    string PlanoCodigo,
    int CotaTotal,
    int CotaUsada,
    int CotaDisponivel);

public sealed record ContratacaoResponse(Guid AssinaturaId);
