using Acme.Sistemas.Infrastructure.Messaging;
using Acme.Sistemas.Services.V1.Exportacao;

namespace Acme.Sistemas.HomeFinance.Api.Config;

/// <summary>
/// Consome a fila de exportações e dispara a geração de cada arquivo.
/// </summary>
/// <remarks>
/// Cada mensagem roda no seu próprio escopo de DI: o gerador assume o contexto do tenant, e
/// reaproveitar escopo entre mensagens deixaria o tenant de uma exportação ativo na seguinte.
/// <para>
/// Indisponibilidade do broker não derruba a Api — a liveness não depende de dependência externa.
/// O consumo tenta de novo depois de uma espera; enquanto isso, os endpoints seguem servindo e as
/// solicitações se acumulam na fila durável, sem se perder.
/// </para>
/// </remarks>
public sealed class ConsumidorDeExportacao(
    IFilaExportacao fila,
    IServiceScopeFactory escopos,
    ILogger<ConsumidorDeExportacao> logger)
    : BackgroundService
{
    private static readonly TimeSpan EsperaAoFalhar = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await fila.ConsumirAsync(ProcessarAsync, stoppingToken);

                // ConsumirAsync registra o consumidor e retorna; manter o serviço vivo é o que
                // sustenta a entrega contínua até o encerramento.
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception excecao)
            {
                logger.LogError(excecao, "Falha ao consumir a fila de exportação; nova tentativa em breve.");

                try
                {
                    await Task.Delay(EsperaAoFalhar, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private async Task ProcessarAsync(MensagemExportacao mensagem, CancellationToken cancellationToken)
    {
        await using var escopo = escopos.CreateAsyncScope();

        await escopo.ServiceProvider
            .GetRequiredService<GeradorDeExportacao>()
            .GerarAsync(mensagem, cancellationToken);
    }
}
