using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Exportacao.Command.HabilitarExportacao;

public sealed record HabilitarExportacaoCommandResult : IResultadoRecusavel<HabilitarExportacaoCommandResult>
{
    public bool Sucesso { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static HabilitarExportacaoCommandResult Habilitada() => new() { Sucesso = true };

    public static HabilitarExportacaoCommandResult Recusada(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };

    static HabilitarExportacaoCommandResult IResultadoRecusavel<HabilitarExportacaoCommandResult>.Recusar(Error erro) => Recusada(erro);
}
