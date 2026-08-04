using Acme.Sistemas.Services.V1.Classificacao.Command.ClassificarLancamento;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.ClassificarLancamento;

internal static class ClassificarLancamentoMap
{
    public static ClassificarLancamentoCommand ParaCommand(
        this ClassificarLancamentoRequest request, Guid tenantId, Guid lancamentoId) =>
        new(tenantId, lancamentoId, request.CategoriaId, request.CriarRegra);

    public static ClassificarLancamentoResponse ParaResponse(
        this ClassificarLancamentoCommandResult resultado) =>
        new(resultado.CompetenciaAfetada!.Value, resultado.RegraId, resultado.PadraoDaRegra);
}
