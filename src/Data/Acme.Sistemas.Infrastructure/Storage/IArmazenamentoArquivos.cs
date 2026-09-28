namespace Acme.Sistemas.Infrastructure.Storage;

/// <summary>
/// Armazenamento de arquivos gerados — o GED da plataforma, sobre MinIO (S3-compatível).
/// </summary>
/// <remarks>
/// Interface na Infrastructure, provider plugável: o padrão do blueprint é MinIO, com AWS/Azure
/// atrás do mesmo contrato. A exportação grava aqui em vez de no banco porque arquivo não é dado
/// relacional — e mantê-lo fora do Postgres é o que permite expirá-lo apagando um objeto, sem
/// inchar a base com blobs que vivem poucos dias.
/// </remarks>
public interface IArmazenamentoArquivos
{
    /// <summary>Grava o conteúdo sob a chave. Sobrescreve se a chave já existir.</summary>
    Task GravarAsync(
        string chave,
        byte[] conteudo,
        string contentType,
        CancellationToken cancellationToken);

    /// <summary>Lê o conteúdo. Nulo quando a chave não existe — arquivo expirado, por exemplo.</summary>
    Task<byte[]?> BaixarAsync(string chave, CancellationToken cancellationToken);

    /// <summary>Remove o objeto. Idempotente: remover o que já não existe não é erro.</summary>
    Task RemoverAsync(string chave, CancellationToken cancellationToken);
}
