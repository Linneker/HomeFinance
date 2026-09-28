namespace Acme.Sistemas.Infrastructure.Messaging;

/// <summary>
/// Mensagem que dispara a geração de uma exportação.
/// </summary>
/// <remarks>
/// Carrega só identificadores — nenhum dado financeiro trafega pela fila. O trabalhador assume o
/// contexto do tenant a partir deles e lê os valores já sob RLS, de onde eles realmente estão.
/// </remarks>
public sealed record MensagemExportacao(Guid SolicitacaoId, Guid TenantId, Guid UsuarioId, int Formato);

/// <summary>
/// Fila da geração assíncrona de exportações, sobre RabbitMQ.
/// </summary>
/// <remarks>
/// A geração é assíncrona porque pode ser demorada — varre o histórico inteiro do tenant — e
/// segurar a requisição HTTP até o arquivo ficar pronto entregaria timeout em vez de arquivo. A
/// requisição só enfileira; quem monta o arquivo é o trabalhador, no seu tempo.
/// </remarks>
public interface IFilaExportacao
{
    Task PublicarAsync(MensagemExportacao mensagem, CancellationToken cancellationToken);

    /// <summary>
    /// Consome a fila, entregando cada mensagem ao processador. A entrega só é confirmada quando
    /// o processador conclui sem erro — falha devolve a mensagem à fila, para não perder uma
    /// exportação por causa de uma indisponibilidade momentânea.
    /// </summary>
    Task ConsumirAsync(
        Func<MensagemExportacao, CancellationToken, Task> processar,
        CancellationToken cancellationToken);
}
