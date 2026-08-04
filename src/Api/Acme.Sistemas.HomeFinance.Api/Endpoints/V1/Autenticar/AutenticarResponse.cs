namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Autenticar;

public sealed record AutenticarResponse(string Token, DateTimeOffset ExpiraEm);
