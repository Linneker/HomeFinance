using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Entities.Planejamento;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Planejamento.Query.FontesRenda;

/// <summary>
/// Fontes de renda do tenant e a renda prevista do mês consultado.
/// </summary>
public sealed class FontesRendaQueryHandler(IFonteRendaReadRepository fontes)
    : IRequestHandler<FontesRendaQuery, FontesRendaQueryResult>
{
    public async Task<FontesRendaQueryResult> HandleAsync(
        FontesRendaQuery request,
        CancellationToken cancellationToken)
    {
        if (FontesRendaQueryValidation.Validar(request).Count > 0)
        {
            return new FontesRendaQueryResult([], 0m);
        }

        var mes = request.Competencia is null
            ? null
            : (DateOnly?)CalendarioFinanceiro.PrimeiroDiaDoMes(request.Competencia.Value);

        var encontradas = await fontes.ListarAsync(mes, cancellationToken);

        var listadas = encontradas
            .Select(fonte => new FonteRendaListada(
                fonte.Id,
                fonte.Nome,
                fonte.Valor,
                fonte.DiaRecebimento,
                fonte.VigenciaInicio,
                fonte.VigenciaFim,
                mes is null ? null : fonte.RecebimentoEm(mes.Value)))
            .ToList();

        var renda = mes is null ? 0m : PrevisaoDeRenda.DoMes(mes.Value, encontradas);

        return new FontesRendaQueryResult(listadas, renda);
    }
}
