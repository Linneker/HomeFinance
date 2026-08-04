using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Entities.Classificacao;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Classificacao.Command.ClassificarLancamento;

/// <summary>
/// Atribui categoria a um lançamento e, se a pessoa aceitar, transforma a escolha em regra.
/// </summary>
/// <remarks>
/// A categoria fica marcada como manual. É essa marca que impede qualquer regra futura de
/// desfazer a decisão — sem ela, seria preciso guardar histórico de alteração para saber quem
/// classificou o quê.
/// <para>
/// Trocar a categoria de um lançamento que já tinha regra substitui a associação daquele
/// padrão em vez de recusar por duplicidade: se a pessoa está corrigindo, a associação
/// anterior é que estava errada. A regra antiga deixa de valer para descrições equivalentes,
/// e os lançamentos já classificados por ela permanecem como estão — reclassificar o passado
/// mudaria meses que a pessoa já conferiu e fechou.
/// </para>
/// </remarks>
public sealed class ClassificarLancamentoCommandHandler(
    ILancamentoWriteRepository lancamentos,
    ICategoriaWriteRepository categorias,
    IRegraClassificacaoWriteRepository regras,
    TimeProvider relogio)
    : IRequestHandler<ClassificarLancamentoCommand, ClassificarLancamentoCommandResult>
{
    public const string CodigoLancamentoNaoEncontrado = "lancamento_nao_encontrado";
    public const string CodigoCategoriaInexistente = "categoria_inexistente";
    public const string CodigoDescricaoSemPadrao = "descricao_sem_padrao";

    public async Task<ClassificarLancamentoCommandResult> HandleAsync(
        ClassificarLancamentoCommand request,
        CancellationToken cancellationToken)
    {
        var erros = ClassificarLancamentoCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return ClassificarLancamentoCommandResult.Recusado(erros[0]);
        }

        var lancamento = await lancamentos.ObterAsync(request.LancamentoId, cancellationToken);

        if (lancamento is null)
        {
            return ClassificarLancamentoCommandResult.Recusado(
                new Error(CodigoLancamentoNaoEncontrado, "Lançamento não encontrado."));
        }

        if (!await categorias.ExisteAsync(request.CategoriaId, cancellationToken))
        {
            return ClassificarLancamentoCommandResult.Recusado(
                new Error(
                    CodigoCategoriaInexistente,
                    "Categoria não encontrada neste espaço.",
                    nameof(request.CategoriaId)));
        }

        var padrao = NormalizadorDeDescricao.Normalizar(lancamento.Descricao);

        if (request.CriarRegra && padrao.Length == 0)
        {
            // Descrição vazia viraria um padrão que casa com tudo e classificaria o tenant
            // inteiro na primeira gravação seguinte.
            return ClassificarLancamentoCommandResult.Recusado(
                new Error(
                    CodigoDescricaoSemPadrao,
                    "A descrição deste lançamento não permite criar uma regra."));
        }

        var categoriaAnterior = lancamento.CategoriaId;

        lancamento.CategoriaId = request.CategoriaId;
        lancamento.OrigemClassificacao = OrigemClassificacao.Manual;
        lancamento.AlteradoEm = relogio.GetUtcNow();

        if (!await lancamentos.AlterarAsync(lancamento, cancellationToken))
        {
            return ClassificarLancamentoCommandResult.Recusado(
                new Error(CodigoLancamentoNaoEncontrado, "Lançamento não encontrado."));
        }

        if (!request.CriarRegra)
        {
            return ClassificarLancamentoCommandResult.Classificado(
                lancamento.DataCompetencia, categoriaAnterior, null, null);
        }

        var regra = new RegraClassificacaoEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = request.TenantId,
            Padrao = padrao,
            CategoriaId = request.CategoriaId,
            CriadoEm = relogio.GetUtcNow(),
        };

        await regras.GravarAsync(regra, cancellationToken);

        return ClassificarLancamentoCommandResult.Classificado(
            lancamento.DataCompetencia, categoriaAnterior, regra.Id, regra.Padrao);
    }
}
