using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.ExternalIntegration.Pagamentos;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Assinaturas;

public sealed record ResultadoDaReconciliacao(int Verificadas, int Corrigidas, int Falhas);

/// <summary>
/// Confere periodicamente, junto aos provedores, a situação das assinaturas vigentes.
/// </summary>
/// <remarks>
/// Existe porque notificação se perde. Sem consulta ativa, uma renovação cuja notificação não
/// chegou deixaria a conta suspensa por tempo indeterminado — e um cancelamento que não chegou a
/// deixaria ativa de graça. Os dois erros são silenciosos: ninguém abre um chamado dizendo "estou
/// usando de graça".
/// <para>
/// Corrige apenas quando o provedor responde. Indisponibilidade não muda nada: derrubar
/// assinaturas porque a API da loja caiu transformaria um incidente deles num incidente nosso,
/// com clientes pagantes bloqueados.
/// </para>
/// </remarks>
public sealed class RotinaDeReconciliacao(
    IAssinaturaWriteRepository assinaturas,
    IEnumerable<IProvedorDePagamento> provedores,
    TimeProvider relogio,
    ILogger<RotinaDeReconciliacao> logger)
{
    public async Task<ResultadoDaReconciliacao> ExecutarAsync(CancellationToken cancellationToken)
    {
        var vigentes = await assinaturas.VigentesParaReconciliacaoAsync(cancellationToken);
        var porCanal = provedores.ToDictionary(provedor => provedor.Canal);
        var agora = relogio.GetUtcNow();

        var verificadas = 0;
        var corrigidas = 0;
        var falhas = 0;

        foreach (var assinatura in vigentes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!porCanal.TryGetValue(assinatura.Canal, out var provedor))
            {
                falhas++;
                continue;
            }

            try
            {
                var confirmacao = await provedor.ConsultarAsync(
                    assinatura.ComprovanteReferencia, cancellationToken);

                verificadas++;

                if (confirmacao is { Confirmado: false, Motivo: "provedor_indisponivel" })
                {
                    falhas++;
                    continue;
                }

                var situacao = confirmacao.Confirmado
                    ? SituacaoAssinatura.Ativa
                    : SituacaoAssinatura.Suspensa;

                var periodo = confirmacao.PeriodoFim ?? assinatura.PeriodoFim;

                // Só grava quando há divergência de fato: uma escrita por assinatura vigente a
                // cada execução encheria o log de auditoria de linhas que não mudaram nada.
                if (assinatura.Situacao == situacao && assinatura.PeriodoFim == periodo)
                {
                    continue;
                }

                assinatura.Situacao = situacao;
                assinatura.PeriodoFim = periodo;
                assinatura.CarenciaAte = situacao == SituacaoAssinatura.Suspensa ? agora : null;
                assinatura.AtualizadoEm = agora;

                await assinaturas.AtualizarSituacaoAsync(assinatura, cancellationToken);
                corrigidas++;
            }
            catch (Exception excecao) when (excecao is not OperationCanceledException)
            {
                falhas++;
                logger.LogError(excecao, "Falha ao reconciliar uma assinatura.");
            }
        }

        logger.LogInformation(
            "Reconciliação: {Verificadas} verificadas, {Corrigidas} corrigidas, {Falhas} falhas.",
            verificadas,
            corrigidas,
            falhas);

        return new ResultadoDaReconciliacao(verificadas, corrigidas, falhas);
    }
}
