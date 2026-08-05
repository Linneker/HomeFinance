using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Classificacao.Command.ClassificarLancamento;

/// <param name="CompetenciaAfetada">Mês cujo consumo de orçamento passa a contar este gasto.</param>
public sealed record ClassificarLancamentoCommandResult : IResultadoRecusavel<ClassificarLancamentoCommandResult>
{
    public bool Sucesso { get; init; }

    public DateOnly? CompetenciaAfetada { get; init; }

    public Guid? CategoriaAnteriorId { get; init; }

    public Guid? RegraId { get; init; }

    /// <summary>Padrão gravado. Devolvido para a tela poder dizer o que passou a valer.</summary>
    public string? PadraoDaRegra { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static ClassificarLancamentoCommandResult Classificado(
        DateOnly competencia, Guid? categoriaAnterior, Guid? regraId, string? padrao) =>
        new()
        {
            Sucesso = true,
            CompetenciaAfetada = competencia,
            CategoriaAnteriorId = categoriaAnterior,
            RegraId = regraId,
            PadraoDaRegra = padrao,
        };

    public static ClassificarLancamentoCommandResult Recusado(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };

    /// <summary>Ponto de recusa usado pelos behaviors genéricos do pipeline.</summary>
    static ClassificarLancamentoCommandResult IResultadoRecusavel<ClassificarLancamentoCommandResult>.Recusar(Error erro) => Recusado(erro);
}
