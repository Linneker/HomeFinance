using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Identidade.Command.CriarConta;

public sealed record CriarContaCommandResult
{
    public bool Sucesso { get; init; }

    public Guid? UsuarioId { get; init; }

    public Guid? TenantId { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static CriarContaCommandResult Criada(Guid usuarioId, Guid tenantId) => new()
    {
        Sucesso = true,
        UsuarioId = usuarioId,
        TenantId = tenantId,
    };

    public static CriarContaCommandResult Recusada(params Error[] erros) => new()
    {
        Sucesso = false,
        Erros = erros,
    };
}
