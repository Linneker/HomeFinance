using Acme.Sistemas.Services.V1.Exportacao;

namespace Acme.Sistemas.HomeFinance.Api.Config;

/// <summary>
/// Remove os arquivos de exportação vencidos uma vez por dia.
/// </summary>
/// <remarks>
/// A hospedagem fica aqui e a decisão do que fazer em <see cref="RotinaDeExpiracao"/>: aqui só
/// existe o relógio. A remoção não pode depender de o dono abrir o app — o prazo do arquivo corre
/// mesmo com a conta parada, e dado sensível não fica hospedado além do combinado.
/// </remarks>
public sealed class RotinaDiariaDeExpiracao(
    IServiceScopeFactory escopos,
    TimeProvider relogio,
    ILogger<RotinaDiariaDeExpiracao> logger)
    : BackgroundService
{
    /// <summary>Hora UTC da varredura. Fora do pico, e antes da rotina de alertas.</summary>
    public static readonly TimeOnly HoraDaExecucao = new(5, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var espera = AteAProximaExecucao(relogio.GetUtcNow());

            try
            {
                await Task.Delay(espera, relogio, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                await using var escopo = escopos.CreateAsyncScope();

                await escopo.ServiceProvider
                    .GetRequiredService<RotinaDeExpiracao>()
                    .ExecutarAsync(stoppingToken);
            }
            catch (Exception excecao) when (excecao is not OperationCanceledException)
            {
                logger.LogError(excecao, "A rotina diária de expiração de exportações falhou.");
            }
        }
    }

    internal static TimeSpan AteAProximaExecucao(DateTimeOffset agora)
    {
        var hoje = agora.Date.Add(HoraDaExecucao.ToTimeSpan());
        var alvo = agora.UtcDateTime < hoje ? hoje : hoje.AddDays(1);

        return alvo - agora.UtcDateTime;
    }
}
