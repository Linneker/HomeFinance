using Acme.Sistemas.Services.V1.Classificacao.Command.AlterarRegra;
using Acme.Sistemas.Services.V1.Classificacao.Query.Regras;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.RegraClassificacao;

internal static class RegraClassificacaoMap
{
    public static AlterarRegraCommand ParaCommand(
        this AlterarRegraRequest request, Guid tenantId, Guid regraId) =>
        new(tenantId, regraId, request.Padrao, request.CategoriaId);

    public static RegrasResponse ParaResponse(this RegrasQueryResult resultado) =>
        new([.. resultado.Regras.Select(regra =>
            new RegraItemResponse(regra.Id, regra.Padrao, regra.CategoriaId, regra.CriadaEm))]);

    public static RegraAlteradaResponse ParaResponse(this AlterarRegraCommandResult resultado) =>
        new(resultado.Padrao!);
}
