using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Services.V1.Exportacao.Command.SolicitarExportacao;

public sealed record SolicitarExportacaoCommandResult : IResultadoRecusavel<SolicitarExportacaoCommandResult>
{
    public bool Sucesso { get; init; }

    public Guid? SolicitacaoId { get; init; }

    /// <summary>Formatos oferecidos. Preenchido quando o pedido foi recusado por formato inválido.</summary>
    public IReadOnlyList<FormatoExportacao> FormatosDisponiveis { get; init; } = [];

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static SolicitarExportacaoCommandResult Solicitada(Guid id) =>
        new() { Sucesso = true, SolicitacaoId = id };

    public static SolicitarExportacaoCommandResult FormatoNaoSuportado(
        IReadOnlyList<FormatoExportacao> disponiveis, Error erro) =>
        new() { Sucesso = false, FormatosDisponiveis = disponiveis, Erros = [erro] };

    public static SolicitarExportacaoCommandResult Recusada(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };

    static SolicitarExportacaoCommandResult IResultadoRecusavel<SolicitarExportacaoCommandResult>.Recusar(Error erro) => Recusada(erro);
}
