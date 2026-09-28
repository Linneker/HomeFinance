using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Exportacao.Command.DesabilitarExportacao;

/// <param name="PendentesCanceladas">Solicitações ainda não concluídas que foram canceladas.</param>
public sealed record DesabilitarExportacaoCommandResult : IResultadoRecusavel<DesabilitarExportacaoCommandResult>
{
    public bool Sucesso { get; init; }

    public int PendentesCanceladas { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static DesabilitarExportacaoCommandResult Desabilitada(int canceladas) =>
        new() { Sucesso = true, PendentesCanceladas = canceladas };

    public static DesabilitarExportacaoCommandResult Recusada(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };

    static DesabilitarExportacaoCommandResult IResultadoRecusavel<DesabilitarExportacaoCommandResult>.Recusar(Error erro) => Recusada(erro);
}
