using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.DefinirOrcamento;

public sealed record DefinirOrcamentoCommandResult
{
    public bool Sucesso { get; init; }

    public Guid? OrcamentoId { get; init; }

    public DateOnly? Competencia { get; init; }

    public decimal TotalPlanejado { get; init; }

    public decimal RendaPrevista { get; init; }

    /// <summary>
    /// Texto do aviso de planejamento acima da renda. Nulo quando não há excesso.
    /// </summary>
    /// <remarks>
    /// Fica fora de <see cref="Erros"/> de propósito: erro impede a gravação, e esta operação
    /// gravou. Misturar os dois faria o cliente tratar um aviso como falha e desfazer o que a
    /// pessoa acabou de salvar.
    /// </remarks>
    public string? Aviso { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static DefinirOrcamentoCommandResult Definido(
        Guid orcamentoId,
        DateOnly competencia,
        decimal totalPlanejado,
        decimal rendaPrevista,
        string? aviso) =>
        new()
        {
            Sucesso = true,
            OrcamentoId = orcamentoId,
            Competencia = competencia,
            TotalPlanejado = totalPlanejado,
            RendaPrevista = rendaPrevista,
            Aviso = aviso,
        };

    public static DefinirOrcamentoCommandResult Recusado(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };
}
