using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Lancamentos.Query.Lancamentos;

/// <summary>Extrato do período na visão escolhida.</summary>
public sealed class LancamentosQueryHandler(ILancamentoReadRepository lancamentos)
    : IRequestHandler<LancamentosQuery, LancamentosQueryResult>
{
    public async Task<LancamentosQueryResult> HandleAsync(
        LancamentosQuery request,
        CancellationToken cancellationToken)
    {
        var erros = LancamentosQueryValidation.Validar(request);

        if (erros.Count > 0)
        {
            return LancamentosQueryResult.Recusada(erros[0]);
        }

        var filtro = new FiltroDeLancamento(
            request.De!.Value, request.Ate!.Value, request.PorCaixa, request.CategoriaId);

        var encontrados = await lancamentos.ListarAsync(filtro, cancellationToken);

        return new LancamentosQueryResult(
            [.. encontrados.Select(lancamento => new LancamentoListado(
                lancamento.Id,
                lancamento.Descricao,
                lancamento.Valor,
                lancamento.DataCompetencia,
                lancamento.DataCaixa,
                lancamento.CategoriaId,
                lancamento.PendenteDeClassificacao,
                lancamento.Tipo,
                lancamento.Parcelamento,
                lancamento.Previsto))],
            request.PorCaixa);
    }
}
