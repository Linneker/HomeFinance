using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Entities.Classificacao;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Classificacao.Command.AlterarRegra;

/// <summary>
/// Altera o padrão ou a categoria de uma regra.
/// </summary>
/// <remarks>
/// Vale só dali para frente. Os lançamentos já classificados por ela mantêm suas categorias —
/// reclassificar o passado mudaria meses que a pessoa já conferiu e fechou, e apagaria
/// correções manuais feitas no meio do caminho.
/// </remarks>
public sealed class AlterarRegraCommandHandler(
    IRegraClassificacaoWriteRepository regras,
    ICategoriaWriteRepository categorias)
    : IRequestHandler<AlterarRegraCommand, AlterarRegraCommandResult>
{
    public const string CodigoRegraNaoEncontrada = "regra_nao_encontrada";
    public const string CodigoCategoriaInexistente = "categoria_inexistente";

    public async Task<AlterarRegraCommandResult> HandleAsync(
        AlterarRegraCommand request,
        CancellationToken cancellationToken)
    {
        var erros = AlterarRegraCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return AlterarRegraCommandResult.Recusada(erros[0]);
        }

        var regra = await regras.ObterAsync(request.RegraId, cancellationToken);

        if (regra is null)
        {
            return AlterarRegraCommandResult.Recusada(
                new Error(CodigoRegraNaoEncontrada, "Regra não encontrada."));
        }

        if (!await categorias.ExisteAsync(request.CategoriaId, cancellationToken))
        {
            return AlterarRegraCommandResult.Recusada(
                new Error(
                    CodigoCategoriaInexistente,
                    "Categoria não encontrada neste espaço.",
                    nameof(request.CategoriaId)));
        }

        regra.Padrao = NormalizadorDeDescricao.Normalizar(request.Padrao);
        regra.CategoriaId = request.CategoriaId;

        return await regras.AlterarAsync(regra, cancellationToken)
            ? AlterarRegraCommandResult.Alterada(regra.Padrao)
            : AlterarRegraCommandResult.Recusada(
                new Error(CodigoRegraNaoEncontrada, "Regra não encontrada."));
    }
}
