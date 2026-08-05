using Acme.Sistemas.Services.V1.Planejamento.Command.AlterarFonteRenda;
using Acme.Sistemas.Services.V1.Planejamento.Command.CadastrarFonteRenda;
using Acme.Sistemas.Services.V1.Planejamento.Query.FontesRenda;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.FonteRenda;

internal static class FonteRendaMap
{
    public static CadastrarFonteRendaCommand ParaCommand(
        this CadastrarFonteRendaRequest request, Guid tenantId, DateOnly hoje) =>
        new(
            tenantId,
            request.Nome,
            request.Valor,
            request.DiaRecebimento,
            request.VigenteAPartirDe ?? hoje);

    public static AlterarFonteRendaCommand ParaCommand(
        this AlterarFonteRendaRequest request, Guid tenantId, Guid fonteId, DateOnly hoje) =>
        new(
            tenantId,
            fonteId,
            request.Nome,
            request.Valor,
            request.DiaRecebimento,
            request.VigenteAPartirDe ?? hoje);

    public static FontesRendaResponse ParaResponse(this FontesRendaQueryResult resultado) =>
        new(
            [.. resultado.Fontes.Select(fonte => new FonteRendaItemResponse(
                fonte.Id,
                fonte.Nome,
                fonte.Valor,
                fonte.DiaRecebimento,
                fonte.VigenciaInicio,
                fonte.VigenciaFim,
                fonte.Recebimento))],
            resultado.RendaPrevista);

    public static FonteRendaCadastradaResponse ParaResponse(
        this CadastrarFonteRendaCommandResult resultado) =>
        new(resultado.FonteId!.Value, resultado.VigenciaInicio!.Value, resultado.RendaPrevista);

    public static FonteRendaAlteradaResponse ParaResponse(
        this AlterarFonteRendaCommandResult resultado) =>
        new(resultado.FonteId!.Value, resultado.AbriuNovaVigencia, resultado.VigenciaInicio!.Value);
}
