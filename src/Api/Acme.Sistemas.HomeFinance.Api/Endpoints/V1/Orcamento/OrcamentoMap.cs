using Acme.Sistemas.Services.V1.Planejamento.Command.CopiarOrcamento;
using Acme.Sistemas.Services.V1.Planejamento.Command.DefinirOrcamento;
using Acme.Sistemas.Services.V1.Planejamento.Query.OrcamentoDoMes;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Orcamento;

internal static class OrcamentoMap
{
    public static DefinirOrcamentoCommand ParaCommand(
        this DefinirOrcamentoRequest request, Guid tenantId, DateOnly competencia) =>
        new(tenantId, request.CategoriaId, competencia, request.ValorPlanejado);

    public static CopiarOrcamentoCommand ParaCommand(
        this CopiarOrcamentoRequest request, Guid tenantId) =>
        new(tenantId, request.Origem, request.Destino, request.Confirmado);

    public static OrcamentoDoMesResponse ParaResponse(this OrcamentoDoMesQueryResult resultado) =>
        new(
            resultado.Competencia,
            [.. resultado.Categorias.Select(item =>
                new OrcamentoItemResponse(item.Id, item.CategoriaId, item.ValorPlanejado))],
            resultado.TotalPlanejado,
            resultado.RendaPrevista,
            resultado.Aviso);

    public static OrcamentoDefinidoResponse ParaResponse(
        this DefinirOrcamentoCommandResult resultado) =>
        new(
            resultado.OrcamentoId!.Value,
            resultado.Competencia!.Value,
            resultado.TotalPlanejado,
            resultado.RendaPrevista,
            resultado.Aviso);

    public static OrcamentoCopiadoResponse ParaResponse(
        this CopiarOrcamentoCommandResult resultado) =>
        new(
            resultado.Destino!.Value,
            resultado.LinhasCopiadas,
            resultado.TotalPlanejado,
            resultado.RendaPrevista,
            resultado.Aviso);
}
