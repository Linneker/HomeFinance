using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Entities.Tenants;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Query.ItensDoConsentimento;

/// <summary>
/// Monta a lista item a item do que ficará visível para quem receber o tenant.
/// </summary>
/// <remarks>
/// Não vai ao banco: a lista é catálogo de produto, igual para todo titular. Ainda assim é uma
/// Query e não uma constante lida pelo endpoint — quando um item passar a depender do plano
/// contratado ou dos módulos ativos da conta, a mudança fica contida neste handler e nenhuma
/// tela precisa ser reescrita.
/// </remarks>
public sealed class ItensDoConsentimentoQueryHandler
    : IRequestHandler<ItensDoConsentimentoQuery, ItensDoConsentimentoQueryResult>
{
    public Task<ItensDoConsentimentoQueryResult> HandleAsync(
        ItensDoConsentimentoQuery request,
        CancellationToken cancellationToken)
    {
        if (ItensDoConsentimentoQueryValidation.Validar(request).Count > 0)
        {
            return Task.FromResult(new ItensDoConsentimentoQueryResult([]));
        }

        var itens = CatalogoDeCompartilhamento.Itens
            .Select(item => new ItemDeConsentimento(
                item.Chave, item.Titulo, item.Descricao, item.Sensivel, item.MarcadoPorPadrao))
            .ToList();

        return Task.FromResult(new ItensDoConsentimentoQueryResult(itens));
    }
}
