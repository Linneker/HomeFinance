namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.FonteRenda;

/// <param name="Recebimento">
/// Dia em que o dinheiro entra no mês consultado, já ajustado a meses curtos. Nulo na consulta
/// do histórico completo.
/// </param>
public sealed record FonteRendaItemResponse(
    Guid Id,
    string Nome,
    decimal Valor,
    int DiaRecebimento,
    DateOnly VigenciaInicio,
    DateOnly? VigenciaFim,
    DateOnly? Recebimento);

public sealed record FontesRendaResponse(
    IReadOnlyList<FonteRendaItemResponse> Fontes,
    decimal RendaPrevista);

public sealed record FonteRendaCadastradaResponse(
    Guid Id,
    DateOnly VigenciaInicio,
    decimal RendaPrevista);

/// <param name="Id">
/// Identificador da vigência que passou a valer — muda quando a alteração abriu uma vigência
/// nova, e a tela precisa passar a editar esta, não a que ficou fechada.
/// </param>
public sealed record FonteRendaAlteradaResponse(
    Guid Id,
    bool AbriuNovaVigencia,
    DateOnly VigenciaInicio);
