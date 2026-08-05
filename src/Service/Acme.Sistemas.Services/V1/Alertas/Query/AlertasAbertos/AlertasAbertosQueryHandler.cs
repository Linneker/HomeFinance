using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Alertas.Query.AlertasAbertos;

/// <summary>
/// Alertas visíveis do tenant ativo.
/// </summary>
/// <remarks>
/// Quem tem apenas leitura não recebe alerta nenhum, e a recusa é aqui — antes de ir ao banco.
/// A RLS deixaria o convidado ler a tabela, porque a policy de SELECT vale para o tenant
/// compartilhado; o que ela não sabe é que alerta não é informação de acompanhamento, é
/// convocação para agir. Quem age é o dono.
/// </remarks>
public sealed class AlertasAbertosQueryHandler(IAlertaReadRepository alertas)
    : IRequestHandler<AlertasAbertosQuery, AlertasAbertosQueryResult>
{
    public async Task<AlertasAbertosQueryResult> HandleAsync(
        AlertasAbertosQuery request,
        CancellationToken cancellationToken)
    {
        if (request.SomenteLeitura || AlertasAbertosQueryValidation.Validar(request).Count > 0)
        {
            return AlertasAbertosQueryResult.Vazio;
        }

        var encontrados = await alertas.ListarAbertosAsync(cancellationToken);

        return new AlertasAbertosQueryResult(
            [.. encontrados.Select(alerta =>
                new AlertaListado(alerta.Id, alerta.Tipo, alerta.Mensagem, alerta.CriadoEm))]);
    }
}
