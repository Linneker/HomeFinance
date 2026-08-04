using Acme.Sistemas.Services.V1.Identidade.Command.ExcluirConta;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.ExcluirConta;

internal static class ExcluirContaMap
{
    public static ExcluirContaCommand ParaCommand(Guid usuarioId, bool confirmacao) =>
        new(usuarioId, confirmacao);

    public static ExcluirContaResponse ParaResponse(this ExcluirContaCommandResult resultado) =>
        new(resultado.Sucesso);
}
