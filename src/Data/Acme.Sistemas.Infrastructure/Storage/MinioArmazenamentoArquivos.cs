using Acme.Sistemas.Infrastructure.Secrets;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Acme.Sistemas.Infrastructure.Storage;

/// <inheritdoc cref="IArmazenamentoArquivos"/>
/// <remarks>
/// Credenciais e endpoint vêm do cofre, resolvidos na primeira operação — nunca de
/// <c>appsettings</c>. O cliente e a garantia de que o bucket existe são montados uma vez e
/// reaproveitados; recriá-los a cada chamada reabriria conexão a troco de nada.
/// </remarks>
public sealed class MinioArmazenamentoArquivos(ISecretProvider segredos) : IArmazenamentoArquivos
{
    public const string ChaveEndpoint = "Minio__Endpoint";
    public const string ChaveAccessKey = "Minio__AccessKey";
    public const string ChaveSecretKey = "Minio__SecretKey";
    public const string ChaveBucket = "Minio__Bucket";
    public const string ChaveSeguro = "Minio__Secure";

    private readonly SemaphoreSlim _semaforo = new(1, 1);

    private IMinioClient? _cliente;
    private string? _bucket;

    public async Task GravarAsync(
        string chave,
        byte[] conteudo,
        string contentType,
        CancellationToken cancellationToken)
    {
        var (cliente, bucket) = await ObterAsync(cancellationToken);

        using var fonte = new MemoryStream(conteudo, writable: false);

        await cliente.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(chave)
                .WithStreamData(fonte)
                .WithObjectSize(conteudo.LongLength)
                .WithContentType(contentType),
            cancellationToken);
    }

    public async Task<byte[]?> BaixarAsync(string chave, CancellationToken cancellationToken)
    {
        var (cliente, bucket) = await ObterAsync(cancellationToken);

        using var destino = new MemoryStream();

        try
        {
            await cliente.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(chave)
                    .WithCallbackStream((fluxo, ct) => fluxo.CopyToAsync(destino, ct)),
                cancellationToken);
        }
        catch (ObjectNotFoundException)
        {
            // Arquivo expirado e já removido. Nulo em vez de exceção: o chamador traduz isso em
            // "não está mais disponível", que é o estado esperado depois do prazo.
            return null;
        }

        return destino.ToArray();
    }

    public async Task RemoverAsync(string chave, CancellationToken cancellationToken)
    {
        var (cliente, bucket) = await ObterAsync(cancellationToken);

        await cliente.RemoveObjectAsync(
            new RemoveObjectArgs().WithBucket(bucket).WithObject(chave),
            cancellationToken);
    }

    private async Task<(IMinioClient Cliente, string Bucket)> ObterAsync(
        CancellationToken cancellationToken)
    {
        if (_cliente is not null && _bucket is not null)
        {
            return (_cliente, _bucket);
        }

        await _semaforo.WaitAsync(cancellationToken);

        try
        {
            if (_cliente is not null && _bucket is not null)
            {
                return (_cliente, _bucket);
            }

            var endpoint = await segredos.GetAsync(ChaveEndpoint, cancellationToken);
            var accessKey = await segredos.GetAsync(ChaveAccessKey, cancellationToken);
            var secretKey = await segredos.GetAsync(ChaveSecretKey, cancellationToken);
            var bucket = await segredos.GetAsync(ChaveBucket, cancellationToken);
            var seguro = string.Equals(
                await segredos.GetAsync(ChaveSeguro, cancellationToken), "true",
                StringComparison.OrdinalIgnoreCase);

            var cliente = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(seguro)
                .Build();

            var existe = await cliente.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(bucket), cancellationToken);

            if (!existe)
            {
                await cliente.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(bucket), cancellationToken);
            }

            _cliente = cliente;
            _bucket = bucket;

            return (cliente, bucket);
        }
        finally
        {
            _semaforo.Release();
        }
    }
}
