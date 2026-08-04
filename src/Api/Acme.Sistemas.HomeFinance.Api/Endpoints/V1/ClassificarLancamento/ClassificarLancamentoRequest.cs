namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.ClassificarLancamento;

/// <param name="CriarRegra">
/// Aceite da oferta de transformar a correção em regra. Falso classifica só aquele lançamento.
/// </param>
public sealed record ClassificarLancamentoRequest(Guid CategoriaId, bool CriarRegra = false);
