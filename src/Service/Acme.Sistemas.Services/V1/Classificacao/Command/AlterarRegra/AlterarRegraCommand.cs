using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Classificacao.Command.AlterarRegra;

/// <param name="Padrao">Texto procurado na descrição. É normalizado antes de gravar.</param>
public sealed record AlterarRegraCommand(
    Guid TenantId,
    Guid RegraId,
    string Padrao,
    Guid CategoriaId) : IRequest<AlterarRegraCommandResult>;
