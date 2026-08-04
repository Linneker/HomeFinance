using Acme.Sistemas.Services.V1.Lancamentos.Command.AlterarLancamento;
using Acme.Sistemas.Services.V1.Lancamentos.Command.CriarLancamento;
using Acme.Sistemas.Services.V1.Lancamentos.Query.Lancamentos;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Lancamento;

internal static class LancamentoMap
{
    public static CriarLancamentoCommand ParaCommand(
        this CriarLancamentoRequest request, Guid tenantId, Guid usuarioId) =>
        new(tenantId,
            usuarioId,
            request.Descricao,
            request.Valor,
            request.DataCompetencia,
            request.DataCaixa,
            request.CategoriaId,
            request.Parcelas,
            request.ConfirmouDuplicidade);

    public static AlterarLancamentoCommand ParaCommand(
        this AlterarLancamentoRequest request, Guid tenantId, Guid lancamentoId) =>
        new(tenantId,
            lancamentoId,
            request.Descricao,
            request.Valor,
            request.DataCompetencia,
            request.DataCaixa,
            request.CategoriaId);

    public static LancamentoCriadoResponse ParaResponse(this CriarLancamentoCommandResult resultado) =>
        new(resultado.LancamentoIds);

    public static PossivelDuplicidadeResponse ParaAvisoDeDuplicidade(
        this CriarLancamentoCommandResult resultado) =>
        new(resultado.SemelhantesEncontrados,
            "Já existe lançamento igual nesta competência. Reenvie confirmando para gravar assim mesmo.");

    public static LancamentoAlteradoResponse ParaResponse(this AlterarLancamentoCommandResult resultado) =>
        new(resultado.CompetenciasAfetadas);

    public static LancamentosResponse ParaResponse(this LancamentosQueryResult resultado) =>
        new([.. resultado.Lancamentos.Select(l => new LancamentoItemResponse(
                l.Id,
                l.Descricao,
                l.Valor,
                l.DataCompetencia,
                l.DataCaixa,
                l.CategoriaId,
                l.PendenteDeClassificacao,
                l.Tipo.ToString(),
                l.Parcelamento,
                l.Previsto))],
            resultado.VisaoPorCaixa,
            resultado.Total);
}
