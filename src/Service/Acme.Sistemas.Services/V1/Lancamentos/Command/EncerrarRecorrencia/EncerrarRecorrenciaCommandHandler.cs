using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.EncerrarRecorrencia;

/// <summary>
/// Encerra a regra e apaga as ocorrências previstas que ainda não aconteceram.
/// </summary>
/// <remarks>
/// O corte é hoje: ocorrência prevista com data futura some, e o que já passou permanece
/// inalterado — inclusive o que ainda estava marcado como previsto e nunca foi confirmado,
/// porque apagá-lo removeria do histórico um gasto que de fato ocorreu.
/// <para>
/// Idempotente: encerrar de novo não encerra nada nem apaga mais nada, porque o UPDATE só
/// alcança recorrência ativa e a remoção só acontece quando ele afeta alguma linha.
/// </para>
/// </remarks>
public sealed class EncerrarRecorrenciaCommandHandler(
    IRecorrenciaWriteRepository recorrencias,
    TimeProvider relogio)
    : IRequestHandler<EncerrarRecorrenciaCommand, EncerrarRecorrenciaCommandResult>
{
    public const string CodigoRecorrenciaNaoEncontrada = "recorrencia_nao_encontrada";
    public const string CodigoJaEncerrada = "recorrencia_ja_encerrada";

    public async Task<EncerrarRecorrenciaCommandResult> HandleAsync(
        EncerrarRecorrenciaCommand request,
        CancellationToken cancellationToken)
    {
        var erros = EncerrarRecorrenciaCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return EncerrarRecorrenciaCommandResult.Recusada(erros[0]);
        }

        var recorrencia = await recorrencias.ObterAsync(request.RecorrenciaId, cancellationToken);

        if (recorrencia is null)
        {
            return EncerrarRecorrenciaCommandResult.Recusada(
                new Error(CodigoRecorrenciaNaoEncontrada, "Recorrência não encontrada."));
        }

        var agora = relogio.GetUtcNow();
        var corte = DateOnly.FromDateTime(agora.UtcDateTime);

        var encerrou = await recorrencias.EncerrarAsync(
            request.RecorrenciaId, corte, agora, cancellationToken);

        return encerrou
            ? EncerrarRecorrenciaCommandResult.Encerrada(corte)
            : EncerrarRecorrenciaCommandResult.Recusada(
                new Error(CodigoJaEncerrada, "Esta recorrência já estava encerrada."));
    }
}
