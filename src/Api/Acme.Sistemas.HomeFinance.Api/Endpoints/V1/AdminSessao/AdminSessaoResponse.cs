namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.AdminSessao;

public sealed record AdminSessaoResponse(string Token, DateTimeOffset ExpiraEm);
