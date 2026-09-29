using Acme.Sistemas.Services.V1.Administracao.Command.AutenticarAdministrador;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.AdminSessao;

internal static class AdminSessaoMap
{
    public static AutenticarAdministradorCommand ParaCommand(this AdminSessaoRequest request) =>
        new(request.Email, request.Senha);

    public static AdminSessaoResponse ParaResponse(this AutenticarAdministradorCommandResult resultado) =>
        new(resultado.Token!, resultado.ExpiraEm!.Value);
}
