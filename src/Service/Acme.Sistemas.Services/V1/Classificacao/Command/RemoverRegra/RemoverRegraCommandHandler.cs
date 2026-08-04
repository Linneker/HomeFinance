using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Classificacao.Command.RemoverRegra;

/// <summary>
/// Remove uma regra do tenant ativo.
/// </summary>
/// <remarks>
/// Lançamentos futuros deixam de ser classificados por ela; os já classificados mantêm suas
/// categorias. Apagar a regra não pode desclassificar meses fechados — o histórico do cliente
/// não muda porque ele mudou de ideia sobre uma regra.
/// </remarks>
public sealed class RemoverRegraCommandHandler(IRegraClassificacaoWriteRepository regras)
    : IRequestHandler<RemoverRegraCommand, RemoverRegraCommandResult>
{
    public const string CodigoRegraNaoEncontrada = "regra_nao_encontrada";

    public async Task<RemoverRegraCommandResult> HandleAsync(
        RemoverRegraCommand request,
        CancellationToken cancellationToken)
    {
        var erros = RemoverRegraCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return RemoverRegraCommandResult.Recusada(erros[0]);
        }

        return await regras.RemoverAsync(request.RegraId, cancellationToken)
            ? RemoverRegraCommandResult.Removida()
            : RemoverRegraCommandResult.Recusada(
                new Error(CodigoRegraNaoEncontrada, "Regra não encontrada."));
    }
}
