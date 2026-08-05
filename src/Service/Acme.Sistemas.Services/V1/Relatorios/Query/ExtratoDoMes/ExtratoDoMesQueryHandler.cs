using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Relatorios.Query.ExtratoDoMes;

/// <summary>
/// Extrato do mês, cada linha ligada à previsão orçamentária que a cobre.
/// </summary>
/// <remarks>
/// O orçamento consultado é sempre o do mês de competência, mesmo na visão de caixa. Não é
/// descuido: o plano é definido por competência e é um número só. Na visão de caixa, portanto,
/// a comparação passa a responder outra pergunta — "quanto do que planejei para agosto de fato
/// saiu em agosto?" —, que é o que separa a compra parcelada do dinheiro que ela consome.
/// </remarks>
public sealed class ExtratoDoMesQueryHandler(
    IRelatorioReadRepository relatorios,
    IOrcamentoReadRepository orcamentos)
    : IRequestHandler<ExtratoDoMesQuery, ExtratoDoMesQueryResult>
{
    public async Task<ExtratoDoMesQueryResult> HandleAsync(
        ExtratoDoMesQuery request,
        CancellationToken cancellationToken)
    {
        var competencia = CalendarioFinanceiro.PrimeiroDiaDoMes(request.Competencia);

        if (ExtratoDoMesQueryValidation.Validar(request).Count > 0)
        {
            return new ExtratoDoMesQueryResult(competencia, request.PorCaixa, [], 0m, 0, true);
        }

        var periodo = new PeriodoDoRelatorio(
            competencia,
            CalendarioFinanceiro.UltimoDiaDoMes(competencia),
            request.PorCaixa);

        var linhas = await relatorios.ExtratoAsync(periodo, cancellationToken);
        var planejado = await orcamentos.ListarDoMesAsync(competencia, cancellationToken);

        var porCategoria = planejado.ToDictionary(item => item.CategoriaId);

        var itens = linhas
            .Select(linha =>
            {
                var orcamento = linha.CategoriaId is not null
                                && porCategoria.TryGetValue(linha.CategoriaId.Value, out var achado)
                    ? achado
                    : null;

                return new ItemDoExtrato(
                    linha.Id,
                    linha.Descricao,
                    linha.ValorApurado,
                    linha.DataCompetencia,
                    linha.DataCaixa,
                    linha.CategoriaId,
                    linha.PendenteDeClassificacao,

                    // Pendente não é "fora do planejamento": ainda não se sabe a que plano o
                    // gasto pertenceria. Marcar as duas coisas juntas empurraria a pessoa a
                    // rever o orçamento quando o que falta é classificar.
                    ForaDoPlanejamento: !linha.PendenteDeClassificacao && orcamento is null,
                    orcamento?.Id,
                    orcamento?.ValorPlanejado,
                    linha.Tipo,
                    linha.Parcelamento,
                    linha.Previsto);
            })
            .ToList();

        return new ExtratoDoMesQueryResult(
            competencia,
            request.PorCaixa,
            itens,
            itens.Sum(item => item.Valor),
            itens.Count(item => item.PendenteDeClassificacao),
            itens.Count == 0);
    }
}
