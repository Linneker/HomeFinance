using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.EncerrarRecorrencia;

public sealed record EncerrarRecorrenciaCommandResult : IResultadoRecusavel<EncerrarRecorrenciaCommandResult>
{
    public bool Sucesso { get; init; }

    /// <summary>Data do corte: ocorrências previstas a partir dela deixaram de existir.</summary>
    public DateOnly? APartirDe { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static EncerrarRecorrenciaCommandResult Encerrada(DateOnly aPartirDe) =>
        new() { Sucesso = true, APartirDe = aPartirDe };

    public static EncerrarRecorrenciaCommandResult Recusada(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };

    /// <summary>Ponto de recusa usado pelos behaviors genéricos do pipeline.</summary>
    static EncerrarRecorrenciaCommandResult IResultadoRecusavel<EncerrarRecorrenciaCommandResult>.Recusar(Error erro) => Recusada(erro);
}
