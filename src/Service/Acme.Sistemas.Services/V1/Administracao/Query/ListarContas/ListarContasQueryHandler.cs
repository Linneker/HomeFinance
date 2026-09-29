using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Administracao.Query.ListarContas;

/// <summary>
/// Monta a relação de contas a partir da leitura administrativa.
/// </summary>
/// <remarks>
/// A fonte é uma função que projeta só colunas de conta e de situação de assinatura — não há por
/// onde um valor financeiro entrar nesta lista.
/// </remarks>
public sealed class ListarContasQueryHandler(IAdministracaoReadRepository contas)
    : IRequestHandler<ListarContasQuery, ListarContasQueryResult>
{
    public async Task<ListarContasQueryResult> HandleAsync(
        ListarContasQuery request,
        CancellationToken cancellationToken)
    {
        var registros = await contas.ListarContasAsync(cancellationToken);

        var resumos = registros
            .Select(conta => new ContaResumo(
                conta.UsuarioId,
                conta.Email,
                conta.CriadoEm,
                conta.Situacao,
                conta.PlanoCodigo,
                conta.PlanoNome))
            .ToList();

        return new ListarContasQueryResult(resumos);
    }
}
