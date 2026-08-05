using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.RemoverLancamento;

public sealed record RemoverLancamentoCommandResult : IResultadoRecusavel<RemoverLancamentoCommandResult>
{
    public bool Sucesso { get; init; }

    /// <summary>Mês cujo consumo de orçamento precisa ser reapurado.</summary>
    public DateOnly? CompetenciaAfetada { get; init; }

    public Guid? CategoriaAfetadaId { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static RemoverLancamentoCommandResult Removido(DateOnly competencia, Guid? categoriaId) =>
        new() { Sucesso = true, CompetenciaAfetada = competencia, CategoriaAfetadaId = categoriaId };

    public static RemoverLancamentoCommandResult Recusado(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };

    /// <summary>Ponto de recusa usado pelos behaviors genéricos do pipeline.</summary>
    static RemoverLancamentoCommandResult IResultadoRecusavel<RemoverLancamentoCommandResult>.Recusar(Error erro) => Recusado(erro);
}
