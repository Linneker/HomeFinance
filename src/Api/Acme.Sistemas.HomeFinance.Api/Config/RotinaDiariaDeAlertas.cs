using Acme.Sistemas.Services.V1.Alertas;

namespace Acme.Sistemas.HomeFinance.Api.Config;

/// <summary>
/// Dispara a rotina de alertas de caixa uma vez por dia.
/// </summary>
/// <remarks>
/// A hospedagem fica na Api e a decisão do que a rotina faz fica em <see cref="RotinaDeAlertasDeCaixa"/>:
/// aqui só existe o relógio. É o que permite testar a rotina sem subir a aplicação e trocar o
/// agendador por um worker dedicado sem tocar na lógica.
/// <para>
/// Cada execução abre o seu próprio escopo de DI. O contexto de isolamento é <c>Scoped</c> e a
/// rotina o reescreve a cada tenant — reaproveitar o escopo entre execuções deixaria o último
/// tenant do dia anterior ativo no começo do dia seguinte.
/// </para>
/// <para>
/// Um único nó executa por vez apenas se houver um único nó. Com mais de uma réplica, a
/// coordenação precisa entrar antes — sem ela, N réplicas reavaliam tudo N vezes. Não duplica
/// alerta (a chave de condição impede), mas multiplica o trabalho por N.
/// </para>
/// </remarks>
public sealed class RotinaDiariaDeAlertas(
    IServiceScopeFactory escopos,
    TimeProvider relogio,
    ILogger<RotinaDiariaDeAlertas> logger)
    : BackgroundService
{
    /// <summary>Hora UTC da execução. Cedo o bastante para o alerta chegar antes do dia começar.</summary>
    public static readonly TimeOnly HoraDaExecucao = new(6, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var espera = AteAProximaExecucao(relogio.GetUtcNow());

            logger.LogInformation("Próxima rotina de alertas em {Espera}.", espera);

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
                    .GetRequiredService<RotinaDeAlertasDeCaixa>()
                    .ExecutarAsync(stoppingToken);
            }
            catch (Exception excecao) when (excecao is not OperationCanceledException)
            {
                // Falhar a rotina não pode derrubar a Api: os endpoints continuam servindo, e
                // a execução do dia seguinte tenta de novo.
                logger.LogError(excecao, "A rotina diária de alertas falhou.");
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
