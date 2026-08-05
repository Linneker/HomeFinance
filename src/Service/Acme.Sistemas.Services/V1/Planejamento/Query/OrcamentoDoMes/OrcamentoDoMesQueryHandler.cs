using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Entities.Planejamento;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Planejamento.Query.OrcamentoDoMes;

/// <summary>
/// Orçamento do mês, com a renda prevista e o aviso de excesso já resolvidos.
/// </summary>
/// <remarks>
/// Categoria sem valor planejado simplesmente não aparece aqui. Quem cruza isso com os gastos e
/// mostra o que ficou fora do planejamento é o relatório mensal — inventar linhas com valor
/// zero neste ponto faria a tela de edição sugerir que a pessoa planejou não gastar, quando na
/// verdade ela não planejou nada.
/// </remarks>
public sealed class OrcamentoDoMesQueryHandler(
    IOrcamentoReadRepository orcamentos,
    IFonteRendaReadRepository fontes)
    : IRequestHandler<OrcamentoDoMesQuery, OrcamentoDoMesQueryResult>
{
    public async Task<OrcamentoDoMesQueryResult> HandleAsync(
        OrcamentoDoMesQuery request,
        CancellationToken cancellationToken)
    {
        var competencia = CalendarioFinanceiro.PrimeiroDiaDoMes(request.Competencia);

        if (OrcamentoDoMesQueryValidation.Validar(request).Count > 0)
        {
            return new OrcamentoDoMesQueryResult(competencia, [], 0m, 0m, null);
        }

        var doMes = await orcamentos.ListarDoMesAsync(competencia, cancellationToken);
        var vigentes = await fontes.ListarAsync(competencia, cancellationToken);

        var total = doMes.Sum(item => item.ValorPlanejado);
        var renda = PrevisaoDeRenda.DoMes(competencia, vigentes);

        return new OrcamentoDoMesQueryResult(
            competencia,
            [.. doMes.Select(item =>
                new OrcamentoDaCategoria(item.Id, item.CategoriaId, item.ValorPlanejado))],
            total,
            renda,
            PrevisaoDeRenda.Avaliar(renda, total)?.Mensagem);
    }
}
