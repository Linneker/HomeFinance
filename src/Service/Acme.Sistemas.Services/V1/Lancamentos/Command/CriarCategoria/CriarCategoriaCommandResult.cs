using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.CriarCategoria;

public sealed record CriarCategoriaCommandResult : IResultadoRecusavel<CriarCategoriaCommandResult>
{
    public bool Sucesso { get; init; }

    public Guid? CategoriaId { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static CriarCategoriaCommandResult Criada(Guid id) =>
        new() { Sucesso = true, CategoriaId = id };

    public static CriarCategoriaCommandResult Recusada(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };

    /// <summary>Ponto de recusa usado pelos behaviors genéricos do pipeline.</summary>
    static CriarCategoriaCommandResult IResultadoRecusavel<CriarCategoriaCommandResult>.Recusar(Error erro) => Recusada(erro);
}
