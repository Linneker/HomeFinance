using Acme.Sistemas.Services.V1.Lancamentos.Command.CriarRecorrencia;
using Acme.Sistemas.Services.V1.Lancamentos.Command.EncerrarRecorrencia;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Recorrencia;

internal static class RecorrenciaMap
{
    public static CriarRecorrenciaCommand ParaCommand(
        this CriarRecorrenciaRequest request, Guid tenantId, Guid usuarioId) =>
        new(tenantId,
            usuarioId,
            request.Descricao,
            request.Valor,
            request.DiaDoMes,
            request.CategoriaId,
            request.Inicio);

    public static RecorrenciaCriadaResponse ParaResponse(this CriarRecorrenciaCommandResult resultado) =>
        new(resultado.RecorrenciaId!.Value, resultado.OcorrenciasGeradas);

    public static RecorrenciaEncerradaResponse ParaResponse(
        this EncerrarRecorrenciaCommandResult resultado) =>
        new(resultado.APartirDe!.Value);
}
