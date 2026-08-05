using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.CriarRecorrencia;

public sealed record CriarRecorrenciaCommandResult : IResultadoRecusavel<CriarRecorrenciaCommandResult>
{
    public bool Sucesso { get; init; }

    public Guid? RecorrenciaId { get; init; }

    /// <summary>Ocorrências previstas materializadas no horizonte.</summary>
    public int OcorrenciasGeradas { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static CriarRecorrenciaCommandResult Criada(Guid id, int ocorrencias) =>
        new() { Sucesso = true, RecorrenciaId = id, OcorrenciasGeradas = ocorrencias };

    public static CriarRecorrenciaCommandResult Recusada(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };

    /// <summary>Ponto de recusa usado pelos behaviors genéricos do pipeline.</summary>
    static CriarRecorrenciaCommandResult IResultadoRecusavel<CriarRecorrenciaCommandResult>.Recusar(Error erro) => Recusada(erro);
}
