using Acme.Sistemas.Services.V1.Alertas.Query.AlertasAbertos;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Alerta;

internal static class AlertaMap
{
    public static AlertasResponse ParaResponse(this AlertasAbertosQueryResult resultado) =>
        new([.. resultado.Alertas.Select(alerta =>
            new AlertaItemResponse(alerta.Id, alerta.Tipo, alerta.Mensagem, alerta.CriadoEm))]);
}
