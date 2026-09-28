using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Exportacao.Query.SituacaoExportacao;

public sealed class SituacaoExportacaoQueryHandler(
    IExportacaoSolicitacaoReadRepository solicitacoes,
    TimeProvider relogio)
    : IRequestHandler<SituacaoExportacaoQuery, SituacaoExportacaoQueryResult>
{
    public async Task<SituacaoExportacaoQueryResult> HandleAsync(
        SituacaoExportacaoQuery request,
        CancellationToken cancellationToken)
    {
        if (SituacaoExportacaoQueryValidation.Validar(request).Count > 0)
        {
            return SituacaoExportacaoQueryResult.NaoEncontrada();
        }

        var solicitacao = await solicitacoes.ObterAsync(request.SolicitacaoId, cancellationToken);

        if (solicitacao is null)
        {
            return SituacaoExportacaoQueryResult.NaoEncontrada();
        }

        var disponivel = solicitacao.Situacao == Acme.Sistemas.Domain.Enums.SituacaoExportacao.Concluida
                         && solicitacao.ReferenciaArquivo is not null
                         && (solicitacao.ExpiraEm is null || solicitacao.ExpiraEm > relogio.GetUtcNow());

        return SituacaoExportacaoQueryResult.De(
            solicitacao.Situacao, solicitacao.Formato, solicitacao.ExpiraEm, disponivel);
    }
}
