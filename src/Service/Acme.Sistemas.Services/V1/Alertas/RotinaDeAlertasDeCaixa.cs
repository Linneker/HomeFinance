using Acme.Sistemas.Domain.Interfaces.Repository;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Alertas;

/// <summary>Contexto de isolamento assumido para trabalhar em nome de um tenant.</summary>
public interface IContextoAssumivel
{
    /// <summary>Assume o tenant como se fosse o dono, para uma unidade de trabalho da rotina.</summary>
    void Assumir(Guid usuarioId, Guid tenantId);
}

public sealed record ResultadoDaRotina(int TenantsAvaliados, int Emitidos, int Encerrados, int Falhas);

/// <summary>
/// Reavalia o alerta de caixa de todos os tenants, uma vez por dia.
/// </summary>
/// <remarks>
/// Existe porque o alerta de caixa depende do tempo passar: uma data coberta ontem pode estar
/// descoberta hoje sem que ninguém tenha tocado no sistema, e sem esta rotina o aviso só
/// apareceria quando a pessoa registrasse algo — possivelmente depois da conta ter voltado.
/// <para>
/// O de competência não precisa de rotina: ele só muda quando um lançamento ou um orçamento
/// muda, e esses momentos já disparam a avaliação.
/// </para>
/// </remarks>
public sealed class RotinaDeAlertasDeCaixa(
    IRotinaReadRepository tenants,
    IContextoAssumivel contexto,
    IAlertaService alertas,
    ILogger<RotinaDeAlertasDeCaixa> logger)
{
    public async Task<ResultadoDaRotina> ExecutarAsync(CancellationToken cancellationToken)
    {
        var ativos = await tenants.TenantsAtivosAsync(cancellationToken);

        var avaliados = 0;
        var emitidos = 0;
        var encerrados = 0;
        var falhas = 0;

        foreach (var tenant in ativos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                contexto.Assumir(tenant.UsuarioId, tenant.TenantId);

                var resultado = await alertas.AvaliarCaixaAsync(cancellationToken);

                avaliados++;
                emitidos += resultado.Emitidos;
                encerrados += resultado.Encerrados;
            }
            catch (Exception excecao) when (excecao is not OperationCanceledException)
            {
                // Um tenant com dado inconsistente não pode impedir a avaliação dos demais: a
                // rotina roda uma vez por dia, e abortar no primeiro erro deixaria toda a base
                // sem alerta até alguém perceber.
                falhas++;
                logger.LogError(excecao, "Falha ao reavaliar o caixa de um tenant.");
            }
        }

        logger.LogInformation(
            "Rotina de caixa: {Avaliados} tenants, {Emitidos} emitidos, {Encerrados} encerrados, {Falhas} falhas.",
            avaliados,
            emitidos,
            encerrados,
            falhas);

        return new ResultadoDaRotina(avaliados, emitidos, encerrados, falhas);
    }
}
