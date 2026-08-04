using System.Data.Common;

namespace Acme.Sistemas.Infrastructure.Databases.Configuration;

/// <summary>
/// Fábrica das conexões da aplicação. A separação Read/Write é o CQRS chegando até o banco —
/// blueprint Acme 2.1.0.
/// </summary>
/// <remarks>
/// Ambos os repositórios têm recuperação e cadastro; o que muda é a conexão. O SELECT que
/// responde a uma Query vai pela <see cref="AbrirLeituraAsync"/>; o SELECT que decide a escrita
/// de um Command vai pela <see cref="AbrirEscritaAsync"/>. Ler pela réplica para decidir uma
/// escrita produz duplicata gravada e atualização perdida quando há atraso de replicação.
/// </remarks>
public interface IDataConfiguration
{
    /// <summary>Conexão de leitura — atende Queries. Réplica quando houver.</summary>
    Task<DbConnection> AbrirLeituraAsync(CancellationToken cancellationToken = default);

    /// <summary>Conexão de escrita — atende Commands, incluindo os SELECTs que decidem a escrita.</summary>
    Task<DbConnection> AbrirEscritaAsync(CancellationToken cancellationToken = default);
}
