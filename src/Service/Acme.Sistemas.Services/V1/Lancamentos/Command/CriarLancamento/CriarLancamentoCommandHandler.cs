using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Services.V1.Classificacao;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.CriarLancamento;

/// <summary>
/// Grava um lançamento — ou o conjunto de parcelas de uma compra — com a participação do dono.
/// </summary>
/// <remarks>
/// A compra parcelada vira N lançamentos irmãos: mesma competência, a da compra, e uma data de
/// caixa por vencimento. Assim a visão de competência soma o valor integral no mês da compra e
/// a de caixa distribui as parcelas, sem que nenhum relatório precise saber que tipo de linha
/// está somando.
/// </remarks>
public sealed class CriarLancamentoCommandHandler(
    ILancamentoWriteRepository lancamentos,
    ICategoriaWriteRepository categorias,
    IClassificacaoService classificacao,
    TimeProvider relogio)
    : IRequestHandler<CriarLancamentoCommand, CriarLancamentoCommandResult>
{
    public const string CodigoCategoriaInexistente = "categoria_inexistente";

    public async Task<CriarLancamentoCommandResult> HandleAsync(
        CriarLancamentoCommand request,
        CancellationToken cancellationToken)
    {
        var erros = CriarLancamentoCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return CriarLancamentoCommandResult.Recusado(erros[0]);
        }

        var descricao = request.Descricao.Trim();
        var competencia = request.DataCompetencia!.Value;
        var primeiroVencimento = request.DataCaixa!.Value;

        if (request.CategoriaId is not null
            && !await categorias.ExisteAsync(request.CategoriaId.Value, cancellationToken))
        {
            // Aceitar a categoria inexistente gravaria o lançamento fora de qualquer orçamento
            // e ele sumiria do resumo por categoria sem aviso nenhum.
            return CriarLancamentoCommandResult.Recusado(
                new Error(
                    CodigoCategoriaInexistente,
                    "Categoria não encontrada neste espaço.",
                    nameof(request.CategoriaId)));
        }

        if (!request.ConfirmouDuplicidade)
        {
            var semelhantes = await lancamentos.ContarSemelhantesAsync(
                descricao, request.Valor, competencia, cancellationToken);

            if (semelhantes > 0)
            {
                return CriarLancamentoCommandResult.PossivelDuplicidade(semelhantes);
            }
        }

        // Categoria informada é escolha da pessoa e vence qualquer regra. Só quando ela não
        // vem é que as regras do tenant entram — classificação automática nunca disputa com
        // decisão manual.
        var categoriaId = request.CategoriaId;
        var origem = OrigemClassificacao.Manual;

        if (categoriaId is null)
        {
            var sugestao = await classificacao.ClassificarAsync(descricao, cancellationToken);

            categoriaId = sugestao.CategoriaId;
            origem = sugestao.Classificou ? OrigemClassificacao.Regra : OrigemClassificacao.Pendente;
        }

        var agora = relogio.GetUtcNow();
        var parcelado = request.Parcelas > 1;
        var grupo = parcelado ? Guid.CreateVersion7() : (Guid?)null;

        var valores = RateioDeParcelas.Dividir(request.Valor, request.Parcelas);
        var vencimentos = CalendarioFinanceiro.SerieMensal(primeiroVencimento, request.Parcelas);

        var novos = new List<LancamentoEntity>(request.Parcelas);
        var participacoes = new List<LancamentoParticipacaoEntity>(request.Parcelas);

        for (var indice = 0; indice < request.Parcelas; indice++)
        {
            var lancamento = new LancamentoEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = request.TenantId,
                Descricao = descricao,
                Valor = valores[indice],

                // A competência é a mesma em todas as parcelas: o gasto aconteceu uma vez.
                DataCompetencia = competencia,
                DataCaixa = vencimentos[indice],

                CategoriaId = categoriaId,
                OrigemClassificacao = origem,
                GrupoParcelamentoId = grupo,
                ParcelaNumero = parcelado ? indice + 1 : null,
                ParcelaTotal = parcelado ? request.Parcelas : null,
                CriadoEm = agora,
            };

            novos.Add(lancamento);

            // Uma participação por lançamento, sempre 100% do dono nesta fase. É daqui que
            // sai a apuração por pessoa nos relatórios, e não do tenant do lançamento.
            participacoes.Add(new LancamentoParticipacaoEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = request.TenantId,
                LancamentoId = lancamento.Id,
                UsuarioId = request.UsuarioId,
                Percentual = LancamentoParticipacaoEntity.PercentualIntegral,
                CriadoEm = agora,
            });
        }

        await lancamentos.CriarAsync(novos, participacoes, cancellationToken);

        return CriarLancamentoCommandResult.Criado([.. novos.Select(lancamento => lancamento.Id)]);
    }
}
