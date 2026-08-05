namespace Acme.Sistemas.Services.V1.Planejamento.Query.FontesRenda;

/// <param name="Recebimento">
/// Dia em que o dinheiro entra no mês consultado, já ajustado a meses curtos. Nulo quando a
/// consulta é do histórico inteiro, porque aí não há um mês de referência.
/// </param>
public sealed record FonteRendaListada(
    Guid Id,
    string Nome,
    decimal Valor,
    int DiaRecebimento,
    DateOnly VigenciaInicio,
    DateOnly? VigenciaFim,
    DateOnly? Recebimento);

/// <param name="RendaPrevista">
/// Soma das fontes vigentes no mês consultado. Zero quando a consulta não é de um mês.
/// </param>
public sealed record FontesRendaQueryResult(
    IReadOnlyList<FonteRendaListada> Fontes,
    decimal RendaPrevista);
