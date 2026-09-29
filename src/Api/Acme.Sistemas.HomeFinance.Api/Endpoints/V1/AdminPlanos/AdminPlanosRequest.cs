namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.AdminPlanos;

/// <summary>Criação ou atualização de um plano. O código identifica o plano.</summary>
public sealed record AdminPlanoRequest(
    string Codigo,
    string Nome,
    int CotaCompartilhamentos,
    decimal PrecoMensal,
    bool Ativo);
