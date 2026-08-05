namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Relatorio;

/// <summary>
/// Recorte do relatório vindo da query string.
/// </summary>
/// <param name="PorCaixa">
/// Anulável, e não <c>bool</c>: um booleano obrigatório faria a Minimal API recusar a
/// requisição que o omite — e omiti-lo é o caso normal, porque competência é a visão padrão.
/// </param>
public sealed record RecorteDoRelatorio(bool? PorCaixa);
