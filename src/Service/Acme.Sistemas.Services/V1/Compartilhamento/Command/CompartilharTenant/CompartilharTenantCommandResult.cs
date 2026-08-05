using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;

public sealed record CompartilharTenantCommandResult
    : IResultadoRecusavel<CompartilharTenantCommandResult>
{
    public bool Sucesso { get; init; }

    public Guid? CompartilhamentoId { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static CompartilharTenantCommandResult Concedido(Guid id) =>
        new() { Sucesso = true, CompartilhamentoId = id };

    public static CompartilharTenantCommandResult Recusado(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };

    /// <summary>Ponto de recusa usado pelos behaviors genéricos do pipeline.</summary>
    static CompartilharTenantCommandResult
        IResultadoRecusavel<CompartilharTenantCommandResult>.Recusar(Error erro) => Recusado(erro);
}
