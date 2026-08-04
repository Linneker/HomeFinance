namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Recorrencia;

/// <param name="DiaDoMes">
/// Aceita 29, 30 e 31: quem paga no último dia do mês precisa poder dizer isso. A
/// materialização recua para o último dia nos meses mais curtos.
/// </param>
public sealed record CriarRecorrenciaRequest(
    string Descricao,
    decimal Valor,
    int DiaDoMes,
    Guid? CategoriaId,
    DateOnly? Inicio);
