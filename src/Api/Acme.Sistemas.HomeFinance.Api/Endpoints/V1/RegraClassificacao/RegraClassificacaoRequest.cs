namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.RegraClassificacao;

/// <param name="Padrao">Texto procurado na descrição. Normalizado antes de gravar.</param>
public sealed record AlterarRegraRequest(string Padrao, Guid CategoriaId);
