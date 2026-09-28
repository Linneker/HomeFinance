using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Exportacao;

public sealed record SolicitarExportacaoResponse(Guid SolicitacaoId);

public sealed record SituacaoExportacaoResponse(
    SituacaoExportacao Situacao,
    FormatoExportacao Formato,
    DateTimeOffset? ExpiraEm,
    bool DisponivelParaDownload);
