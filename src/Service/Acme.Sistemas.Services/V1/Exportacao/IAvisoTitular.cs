using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Exportacao;

/// <summary>
/// Avisa o titular de um evento sobre a própria conta.
/// </summary>
/// <remarks>
/// Deliberadamente fora do <c>AssinaturaBehavior</c>: o aviso de exportação tem que chegar mesmo
/// com a assinatura suspensa — a exportação existe justamente para o titular que formalizou uma
/// requisição de acesso, e prendê-la à assinatura contradiria isso. Por ser chamada direta, e não
/// um recurso derivado do pipeline, ela nunca é barrada pela situação da assinatura.
/// <para>
/// A entrega ao dispositivo (push) é da superfície do usuário — fase do aplicativo MAUI. Aqui fica
/// o ponto de emissão, que é o que a fase de núcleo garante existir e ser sempre acionado.
/// </para>
/// </remarks>
public interface IAvisoTitular
{
    Task AvisarAsync(Guid usuarioId, string titulo, string mensagem, CancellationToken cancellationToken);
}

/// <summary>Emissão por log, enquanto a entrega ao dispositivo não existe.</summary>
public sealed class AvisoTitularPorLog(ILogger<AvisoTitularPorLog> logger) : IAvisoTitular
{
    public Task AvisarAsync(
        Guid usuarioId,
        string titulo,
        string mensagem,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Aviso ao titular {Usuario}: {Titulo} — {Mensagem}", usuarioId, titulo, mensagem);

        return Task.CompletedTask;
    }
}
