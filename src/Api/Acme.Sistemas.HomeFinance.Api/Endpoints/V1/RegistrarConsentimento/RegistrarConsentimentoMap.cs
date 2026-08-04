using Acme.Sistemas.Services.V1.Identidade.Command.RegistrarConsentimento;
using Acme.Sistemas.Services.V1.Identidade.Query.ConsentimentoVigente;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.RegistrarConsentimento;

internal static class RegistrarConsentimentoMap
{
    public static RegistrarConsentimentoCommand ParaCommand(
        this RegistrarConsentimentoRequest request, Guid usuarioId) =>
        new(usuarioId, request.Documento, request.Versao);

    public static RegistrarConsentimentoResponse ParaResponse(this ConsentimentoVigenteQueryResult resultado) =>
        new(resultado.Vigente, resultado.Pendentes);
}
