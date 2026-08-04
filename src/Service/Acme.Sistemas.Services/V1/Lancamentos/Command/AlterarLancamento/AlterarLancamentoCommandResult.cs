using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.AlterarLancamento;

/// <param name="CompetenciaAfetada">
/// Meses cujo consumo de orçamento precisa ser reapurado. São dois quando a alteração mudou a
/// competência: o mês de onde o valor saiu e o mês para onde foi.
/// </param>
public sealed record AlterarLancamentoCommandResult
{
    public bool Sucesso { get; init; }

    public IReadOnlyList<DateOnly> CompetenciasAfetadas { get; init; } = [];

    public Guid? CategoriaAnteriorId { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static AlterarLancamentoCommandResult Alterado(
        IReadOnlyList<DateOnly> competencias, Guid? categoriaAnterior) =>
        new()
        {
            Sucesso = true,
            CompetenciasAfetadas = competencias,
            CategoriaAnteriorId = categoriaAnterior,
        };

    public static AlterarLancamentoCommandResult Recusado(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };
}
