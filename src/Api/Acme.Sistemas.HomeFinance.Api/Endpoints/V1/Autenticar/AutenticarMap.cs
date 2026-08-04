using Acme.Sistemas.Services.V1.Identidade.Command.Autenticar;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Autenticar;

internal static class AutenticarMap
{
    public static AutenticarCommand ParaCommand(this AutenticarRequest request) =>
        new(request.Email, request.Senha);

    public static AutenticarResponse ParaResponse(this AutenticarCommandResult resultado) =>
        new(resultado.Token!, resultado.ExpiraEm!.Value);
}
