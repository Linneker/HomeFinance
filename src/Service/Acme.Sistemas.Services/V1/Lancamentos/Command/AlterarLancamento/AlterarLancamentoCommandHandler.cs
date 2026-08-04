using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.AlterarLancamento;

/// <summary>
/// Altera um lançamento do tenant ativo.
/// </summary>
/// <remarks>
/// Devolve as competências afetadas para que o consumo de orçamento seja reapurado. Quando a
/// alteração muda o mês de competência são duas: o mês de onde o valor saiu e o mês para onde
/// foi. Reapurar só o novo deixaria o mês antigo estourado para sempre, com um gasto que já
/// não está lá.
/// <para>
/// A carga do estado atual vem do repositório de escrita: decide a alteração, e ler de réplica
/// atrasada devolveria a competência anterior errada — logo, o mês errado a reapurar.
/// </para>
/// </remarks>
public sealed class AlterarLancamentoCommandHandler(
    ILancamentoWriteRepository lancamentos,
    ICategoriaWriteRepository categorias,
    TimeProvider relogio)
    : IRequestHandler<AlterarLancamentoCommand, AlterarLancamentoCommandResult>
{
    public const string CodigoLancamentoNaoEncontrado = "lancamento_nao_encontrado";
    public const string CodigoCategoriaInexistente = "categoria_inexistente";
    public const string CodigoOcorrenciaPrevista = "ocorrencia_prevista_nao_editavel";

    public async Task<AlterarLancamentoCommandResult> HandleAsync(
        AlterarLancamentoCommand request,
        CancellationToken cancellationToken)
    {
        var erros = AlterarLancamentoCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return AlterarLancamentoCommandResult.Recusado(erros[0]);
        }

        var atual = await lancamentos.ObterAsync(request.LancamentoId, cancellationToken);

        if (atual is null)
        {
            return AlterarLancamentoCommandResult.Recusado(
                new Error(CodigoLancamentoNaoEncontrado, "Lançamento não encontrado."));
        }

        if (atual.Previsto)
        {
            // Editar uma ocorrência ainda prevista seria trabalho perdido: a próxima
            // materialização da recorrência a regeraria por cima.
            return AlterarLancamentoCommandResult.Recusado(
                new Error(
                    CodigoOcorrenciaPrevista,
                    "Esta ocorrência ainda é uma previsão da recorrência. Altere a recorrência."));
        }

        if (request.CategoriaId is not null
            && !await categorias.ExisteAsync(request.CategoriaId.Value, cancellationToken))
        {
            return AlterarLancamentoCommandResult.Recusado(
                new Error(
                    CodigoCategoriaInexistente,
                    "Categoria não encontrada neste espaço.",
                    nameof(request.CategoriaId)));
        }

        var competenciaAnterior = atual.DataCompetencia;
        var categoriaAnterior = atual.CategoriaId;

        atual.Descricao = request.Descricao.Trim();
        atual.Valor = request.Valor;
        atual.DataCompetencia = request.DataCompetencia!.Value;
        atual.DataCaixa = request.DataCaixa!.Value;
        atual.CategoriaId = request.CategoriaId;

        // Escolha vinda de uma tela é sempre manual, inclusive a de limpar a categoria.
        // É isso que impede uma regra de classificação de desfazer a decisão da pessoa.
        atual.OrigemClassificacao = request.CategoriaId is null
            ? OrigemClassificacao.Pendente
            : OrigemClassificacao.Manual;

        atual.AlteradoEm = relogio.GetUtcNow();

        if (!await lancamentos.AlterarAsync(atual, cancellationToken))
        {
            return AlterarLancamentoCommandResult.Recusado(
                new Error(CodigoLancamentoNaoEncontrado, "Lançamento não encontrado."));
        }

        var competencias = competenciaAnterior == atual.DataCompetencia
            ? new[] { atual.DataCompetencia }
            : [competenciaAnterior, atual.DataCompetencia];

        return AlterarLancamentoCommandResult.Alterado(competencias, categoriaAnterior);
    }
}
