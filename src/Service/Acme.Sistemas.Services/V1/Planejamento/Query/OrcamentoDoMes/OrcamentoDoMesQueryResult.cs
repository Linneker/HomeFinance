namespace Acme.Sistemas.Services.V1.Planejamento.Query.OrcamentoDoMes;

public sealed record OrcamentoDaCategoria(Guid Id, Guid CategoriaId, decimal ValorPlanejado);

/// <param name="Aviso">
/// Texto do aviso de planejamento acima da renda, ou nulo. Vem junto da consulta porque a tela
/// do mês precisa mostrá-lo já ao abrir, não só depois da próxima edição.
/// </param>
public sealed record OrcamentoDoMesQueryResult(
    DateOnly Competencia,
    IReadOnlyList<OrcamentoDaCategoria> Categorias,
    decimal TotalPlanejado,
    decimal RendaPrevista,
    string? Aviso);
