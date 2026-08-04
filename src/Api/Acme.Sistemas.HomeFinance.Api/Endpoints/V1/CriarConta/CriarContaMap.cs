using Acme.Sistemas.Services.V1.Identidade.Command.CriarConta;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.CriarConta;

internal static class CriarContaMap
{
    public static CriarContaCommand ParaCommand(this CriarContaRequest request) =>
        new(request.Email, request.Senha, request.AceitouTermos, request.VersaoDocumentos);

    public static CriarContaResponse ParaResponse(this CriarContaCommandResult resultado) =>
        new(resultado.UsuarioId!.Value, resultado.TenantId!.Value);
}
