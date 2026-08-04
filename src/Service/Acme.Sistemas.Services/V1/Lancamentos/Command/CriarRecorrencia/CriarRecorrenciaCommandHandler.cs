using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.CriarRecorrencia;

/// <summary>
/// Cria a regra de repetição mensal e materializa o horizonte de ocorrências previstas.
/// </summary>
/// <remarks>
/// As ocorrências são gravadas como linhas, e não calculadas na hora de consultar, porque a
/// projeção de saldo do alerta de caixa precisa que as saídas futuras existam como lançamentos
/// consultáveis. Calcular a recorrência a cada projeção seria mais elegante e bem mais caro.
/// <para>
/// Todas nascem com <c>Previsto</c>: entram na projeção, não no realizado. É essa marca que
/// permite ao encerramento apagar o futuro sem tocar no que já virou gasto.
/// </para>
/// </remarks>
public sealed class CriarRecorrenciaCommandHandler(
    IRecorrenciaWriteRepository recorrencias,
    ICategoriaWriteRepository categorias,
    TimeProvider relogio)
    : IRequestHandler<CriarRecorrenciaCommand, CriarRecorrenciaCommandResult>
{
    public const string CodigoCategoriaInexistente = "categoria_inexistente";

    public async Task<CriarRecorrenciaCommandResult> HandleAsync(
        CriarRecorrenciaCommand request,
        CancellationToken cancellationToken)
    {
        var erros = CriarRecorrenciaCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return CriarRecorrenciaCommandResult.Recusada(erros[0]);
        }

        if (request.CategoriaId is not null
            && !await categorias.ExisteAsync(request.CategoriaId.Value, cancellationToken))
        {
            return CriarRecorrenciaCommandResult.Recusada(
                new Error(
                    CodigoCategoriaInexistente,
                    "Categoria não encontrada neste espaço.",
                    nameof(request.CategoriaId)));
        }

        var agora = relogio.GetUtcNow();

        var recorrencia = new RecorrenciaEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = request.TenantId,
            Descricao = request.Descricao.Trim(),
            Valor = request.Valor,
            DiaDoMes = request.DiaDoMes,
            CategoriaId = request.CategoriaId,
            Inicio = request.Inicio!.Value,
            CriadoEm = agora,
        };

        var primeira = CalendarioFinanceiro.PrimeiraOcorrencia(recorrencia.Inicio, recorrencia.DiaDoMes);

        var datas = CalendarioFinanceiro.SerieMensal(primeira, RecorrenciaEntity.MesesDeHorizonte);

        var ocorrencias = new List<LancamentoEntity>(datas.Count);
        var participacoes = new List<LancamentoParticipacaoEntity>(datas.Count);

        foreach (var data in datas)
        {
            var ocorrencia = new LancamentoEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = request.TenantId,
                Descricao = recorrencia.Descricao,
                Valor = recorrencia.Valor,

                // Conta fixa consome e vence no mesmo dia: não há defasagem entre o serviço
                // usado e o pagamento, ao contrário da fatura de cartão.
                DataCompetencia = data,
                DataCaixa = data,

                CategoriaId = recorrencia.CategoriaId,
                OrigemClassificacao = recorrencia.CategoriaId is null
                    ? OrigemClassificacao.Pendente
                    : OrigemClassificacao.Manual,
                RecorrenciaId = recorrencia.Id,
                Previsto = true,
                CriadoEm = agora,
            };

            ocorrencias.Add(ocorrencia);

            participacoes.Add(new LancamentoParticipacaoEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = request.TenantId,
                LancamentoId = ocorrencia.Id,
                UsuarioId = request.UsuarioId,
                Percentual = LancamentoParticipacaoEntity.PercentualIntegral,
                CriadoEm = agora,
            });
        }

        await recorrencias.CriarAsync(recorrencia, ocorrencias, participacoes, cancellationToken);

        return CriarRecorrenciaCommandResult.Criada(recorrencia.Id, ocorrencias.Count);
    }
}
