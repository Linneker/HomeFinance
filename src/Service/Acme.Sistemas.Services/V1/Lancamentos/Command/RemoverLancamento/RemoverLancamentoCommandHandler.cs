using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.RemoverLancamento;

/// <summary>
/// Remove um lançamento do tenant ativo.
/// </summary>
/// <remarks>
/// Carrega antes de apagar para devolver a competência e a categoria afetadas: depois do
/// DELETE não há mais de onde tirá-las, e sem elas o consumo de orçamento continuaria contando
/// um gasto que já não existe.
/// <para>
/// As participações somem junto, por cascata da chave estrangeira — participação órfã seria
/// somada por relatório nenhum e ainda assim ocuparia a tabela.
/// </para>
/// </remarks>
public sealed class RemoverLancamentoCommandHandler(ILancamentoWriteRepository lancamentos)
    : IRequestHandler<RemoverLancamentoCommand, RemoverLancamentoCommandResult>
{
    public const string CodigoLancamentoNaoEncontrado = "lancamento_nao_encontrado";

    public async Task<RemoverLancamentoCommandResult> HandleAsync(
        RemoverLancamentoCommand request,
        CancellationToken cancellationToken)
    {
        var erros = RemoverLancamentoCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return RemoverLancamentoCommandResult.Recusado(erros[0]);
        }

        var atual = await lancamentos.ObterAsync(request.LancamentoId, cancellationToken);

        if (atual is null)
        {
            return RemoverLancamentoCommandResult.Recusado(
                new Error(CodigoLancamentoNaoEncontrado, "Lançamento não encontrado."));
        }

        if (!await lancamentos.RemoverAsync(request.LancamentoId, cancellationToken))
        {
            return RemoverLancamentoCommandResult.Recusado(
                new Error(CodigoLancamentoNaoEncontrado, "Lançamento não encontrado."));
        }

        return RemoverLancamentoCommandResult.Removido(atual.DataCompetencia, atual.CategoriaId);
    }
}
