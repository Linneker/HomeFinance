namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.CriarConta;

public sealed record CriarContaRequest(string Email, string Senha, bool AceitouTermos, string VersaoDocumentos);
