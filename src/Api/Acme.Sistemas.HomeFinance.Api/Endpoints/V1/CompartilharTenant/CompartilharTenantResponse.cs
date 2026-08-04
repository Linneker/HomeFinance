namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.CompartilharTenant;

public sealed record CompartilharTenantResponse(Guid CompartilhamentoId);

public sealed record ItemDeConsentimentoResponse(
    string Chave,
    string Titulo,
    string Descricao,
    bool Sensivel,
    bool MarcadoPorPadrao);

public sealed record ItensDoConsentimentoResponse(IReadOnlyList<ItemDeConsentimentoResponse> Itens);
