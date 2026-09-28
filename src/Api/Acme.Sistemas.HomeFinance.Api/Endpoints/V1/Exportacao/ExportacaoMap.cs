using Acme.Sistemas.Services.V1.Exportacao.Query.SituacaoExportacao;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Exportacao;

internal static class ExportacaoMap
{
    public static SituacaoExportacaoResponse ParaResponse(this SituacaoExportacaoQueryResult resultado) =>
        new(resultado.Situacao, resultado.Formato, resultado.ExpiraEm, resultado.DisponivelParaDownload);
}
