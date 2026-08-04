using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Classificacao.Query.Regras;

/// <summary>
/// Regras do tenant ativo, na ordem de precedência em que são aplicadas.
/// </summary>
/// <remarks>
/// Exibir na ordem de aplicação é o que permite à pessoa entender por que uma compra caiu numa
/// categoria e não em outra quando duas regras casam.
/// </remarks>
public sealed class RegrasQueryHandler(IRegraClassificacaoReadRepository regras)
    : IRequestHandler<RegrasQuery, RegrasQueryResult>
{
    public async Task<RegrasQueryResult> HandleAsync(
        RegrasQuery request,
        CancellationToken cancellationToken)
    {
        if (RegrasQueryValidation.Validar(request).Count > 0)
        {
            return new RegrasQueryResult([]);
        }

        var encontradas = await regras.ListarAsync(cancellationToken);

        return new RegrasQueryResult(
            [.. encontradas.Select(regra =>
                new RegraListada(regra.Id, regra.Padrao, regra.CategoriaId, regra.CriadoEm))]);
    }
}
