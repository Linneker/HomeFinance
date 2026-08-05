using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.CopiarOrcamento;

public sealed record CopiarOrcamentoCommandResult : IResultadoRecusavel<CopiarOrcamentoCommandResult>
{
    public bool Sucesso { get; init; }

    public int LinhasCopiadas { get; init; }

    public DateOnly? Destino { get; init; }

    public decimal TotalPlanejado { get; init; }

    public decimal RendaPrevista { get; init; }

    public string? Aviso { get; init; }

    /// <summary>
    /// Verdadeiro quando a cópia parou porque o destino já tinha valores. O cliente reenvia a
    /// mesma requisição com <c>Confirmado</c> para prosseguir.
    /// </summary>
    public bool RequerConfirmacao { get; init; }

    /// <summary>Quantas linhas seriam substituídas. Preenchido junto com a confirmação pedida.</summary>
    public int LinhasNoDestino { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static CopiarOrcamentoCommandResult Copiado(
        DateOnly destino,
        int linhas,
        decimal totalPlanejado,
        decimal rendaPrevista,
        string? aviso) =>
        new()
        {
            Sucesso = true,
            Destino = destino,
            LinhasCopiadas = linhas,
            TotalPlanejado = totalPlanejado,
            RendaPrevista = rendaPrevista,
            Aviso = aviso,
        };

    public static CopiarOrcamentoCommandResult ConfirmacaoNecessaria(
        DateOnly destino,
        int linhasNoDestino,
        Error erro) =>
        new()
        {
            Sucesso = false,
            RequerConfirmacao = true,
            Destino = destino,
            LinhasNoDestino = linhasNoDestino,
            Erros = [erro],
        };

    public static CopiarOrcamentoCommandResult Recusado(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };

    /// <summary>Ponto de recusa usado pelos behaviors genéricos do pipeline.</summary>
    static CopiarOrcamentoCommandResult IResultadoRecusavel<CopiarOrcamentoCommandResult>.Recusar(Error erro) => Recusado(erro);
}
