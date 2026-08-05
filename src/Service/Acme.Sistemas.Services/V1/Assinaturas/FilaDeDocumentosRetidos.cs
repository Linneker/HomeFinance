using Acme.Sistemas.Domain.Entities.Assinaturas;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Assinaturas;

public sealed record ResultadoDaFila(int Liberados);

/// <summary>
/// Retém documentos durante a suspensão e libera a fila quando a assinatura regulariza.
/// </summary>
/// <remarks>
/// Não há produtor nesta fase: o envio de documentos é da fase 2. A fila existe agora porque a
/// política de retenção é decisão de assinatura, não de OCR — decidi-la aqui, junto do resto,
/// evita que ela apareça como remendo quando o processamento chegar.
/// <para>
/// A liberação é disparada pela regularização, e não por rotina: "sem exigir ação adicional do
/// usuário além do pagamento" significa que o efeito acontece no momento em que o pagamento é
/// confirmado, não no próximo ciclo de uma rotina.
/// </para>
/// </remarks>
public sealed class FilaDeDocumentosRetidos(
    IDocumentoRetidoRepository documentos,
    IAssinaturaService assinaturas,
    TimeProvider relogio,
    ILogger<FilaDeDocumentosRetidos> logger)
{
    /// <summary>
    /// Registra o documento. Com assinatura vigente ele já entra processável; suspenso, fica
    /// pendente e nenhum processamento é executado.
    /// </summary>
    public async Task<bool> ReceberAsync(
        Guid tenantId,
        string referencia,
        string tipo,
        CancellationToken cancellationToken)
    {
        var situacao = await assinaturas.SituacaoAtualAsync(cancellationToken);
        var agora = relogio.GetUtcNow();

        await documentos.ReterAsync(
            new DocumentoRetidoEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantId,
                Referencia = referencia,
                Tipo = tipo,
                CriadoEm = agora,
                ProcessadoEm = null,
            },
            cancellationToken);

        return situacao.Vigente;
    }

    /// <summary>Libera o que estava retido. Chamado quando a assinatura volta a vigorar.</summary>
    public async Task<ResultadoDaFila> LiberarAsync(CancellationToken cancellationToken)
    {
        var situacao = await assinaturas.SituacaoAtualAsync(cancellationToken);

        if (!situacao.Vigente)
        {
            return new ResultadoDaFila(0);
        }

        var pendentes = await documentos.PendentesAsync(cancellationToken);

        if (pendentes.Count == 0)
        {
            return new ResultadoDaFila(0);
        }

        var liberados = await documentos.MarcarProcessadosAsync(
            [.. pendentes.Select(documento => documento.Id)],
            relogio.GetUtcNow(),
            cancellationToken);

        logger.LogInformation("Fila retida liberada: {Liberados} documento(s).", liberados);

        return new ResultadoDaFila(liberados);
    }
}
