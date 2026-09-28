using System.Text;
using System.Text.Json;
using Acme.Sistemas.Infrastructure.Secrets;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Acme.Sistemas.Infrastructure.Messaging;

/// <inheritdoc cref="IFilaExportacao"/>
/// <remarks>
/// A conexão é resolvida do cofre na primeira operação e reaproveitada. Cada publicação abre e
/// fecha o próprio canal: <c>IChannel</c> não é seguro para publicações concorrentes, e uma
/// requisição não pode contaminar o canal de outra. O consumo abre um canal próprio e longevo.
/// </remarks>
public sealed class RabbitMqFilaExportacao(ISecretProvider segredos) : IFilaExportacao, IAsyncDisposable
{
    public const string ChaveUri = "RabbitMq__Uri";

    /// <summary>Fila durável: mensagem enfileirada sobrevive a um reinício do broker.</summary>
    public const string Fila = "exportacao.geracao";

    private readonly SemaphoreSlim _semaforo = new(1, 1);

    private IConnection? _conexao;

    public async Task PublicarAsync(MensagemExportacao mensagem, CancellationToken cancellationToken)
    {
        var conexao = await ObterConexaoAsync(cancellationToken);

        await using var canal = await conexao.CreateChannelAsync(cancellationToken: cancellationToken);

        await DeclararFilaAsync(canal, cancellationToken);

        var corpo = JsonSerializer.SerializeToUtf8Bytes(mensagem);

        var propriedades = new BasicProperties { Persistent = true };

        await canal.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: Fila,
            mandatory: false,
            basicProperties: propriedades,
            body: corpo,
            cancellationToken: cancellationToken);
    }

    public async Task ConsumirAsync(
        Func<MensagemExportacao, CancellationToken, Task> processar,
        CancellationToken cancellationToken)
    {
        var conexao = await ObterConexaoAsync(cancellationToken);

        var canal = await conexao.CreateChannelAsync(cancellationToken: cancellationToken);

        await DeclararFilaAsync(canal, cancellationToken);

        // Uma mensagem por vez: sem isto o broker despacharia a fila inteira de uma vez, e uma
        // geração pesada travaria as demais sem paralelismo real.
        await canal.BasicQosAsync(0, 1, false, cancellationToken);

        var consumidor = new AsyncEventingBasicConsumer(canal);

        consumidor.ReceivedAsync += async (_, entrega) =>
        {
            try
            {
                var mensagem = JsonSerializer.Deserialize<MensagemExportacao>(entrega.Body.Span)
                    ?? throw new InvalidOperationException("Mensagem de exportação vazia.");

                await processar(mensagem, cancellationToken);

                await canal.BasicAckAsync(entrega.DeliveryTag, multiple: false, cancellationToken);
            }
            catch
            {
                // Devolve à fila: uma indisponibilidade momentânea do banco ou do storage não
                // pode fazer a exportação sumir. requeue evita perder o pedido do titular.
                await canal.BasicNackAsync(
                    entrega.DeliveryTag, multiple: false, requeue: true, cancellationToken);
            }
        };

        await canal.BasicConsumeAsync(
            queue: Fila,
            autoAck: false,
            consumer: consumidor,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_conexao is not null)
        {
            await _conexao.DisposeAsync();
        }

        _semaforo.Dispose();
    }

    private static Task DeclararFilaAsync(IChannel canal, CancellationToken cancellationToken) =>
        canal.QueueDeclareAsync(
            queue: Fila,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

    private async Task<IConnection> ObterConexaoAsync(CancellationToken cancellationToken)
    {
        if (_conexao is { IsOpen: true })
        {
            return _conexao;
        }

        await _semaforo.WaitAsync(cancellationToken);

        try
        {
            if (_conexao is { IsOpen: true })
            {
                return _conexao;
            }

            var uri = await segredos.GetAsync(ChaveUri, cancellationToken);
            var fabrica = new ConnectionFactory { Uri = new Uri(uri) };

            _conexao = await fabrica.CreateConnectionAsync(cancellationToken);

            return _conexao;
        }
        finally
        {
            _semaforo.Release();
        }
    }
}
