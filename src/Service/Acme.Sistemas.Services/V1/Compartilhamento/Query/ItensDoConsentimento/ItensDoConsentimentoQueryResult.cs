namespace Acme.Sistemas.Services.V1.Compartilhamento.Query.ItensDoConsentimento;

/// <param name="MarcadoPorPadrao">
/// Estado inicial da caixa na tela. Falso nos sensíveis — incluí-los exige ação deliberada do
/// dono, e uma tela que já nascesse marcada tornaria o opt-in decorativo.
/// </param>
public sealed record ItemDeConsentimento(
    string Chave,
    string Titulo,
    string Descricao,
    bool Sensivel,
    bool MarcadoPorPadrao);

public sealed record ItensDoConsentimentoQueryResult(IReadOnlyList<ItemDeConsentimento> Itens);
