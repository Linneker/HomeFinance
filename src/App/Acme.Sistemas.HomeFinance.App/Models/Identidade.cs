namespace Acme.Sistemas.HomeFinance.App.Models;

public sealed record SessaoResposta(string Token, DateTimeOffset ExpiraEm);

public sealed record CriarContaResposta(Guid UsuarioId, Guid TenantId);

/// <summary>Situação do consentimento: se está vigente e quais documentos faltam aceitar.</summary>
public sealed record ConsentimentoVigencia(bool Vigente, IReadOnlyList<string> Pendentes);
