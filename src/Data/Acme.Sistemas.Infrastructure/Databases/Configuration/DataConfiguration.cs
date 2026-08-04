using System.Data.Common;
using Acme.Sistemas.Infrastructure.Secrets;
using Npgsql;

namespace Acme.Sistemas.Infrastructure.Databases.Configuration;

/// <summary>
/// Implementação do <see cref="IDataConfiguration"/> sobre PostgreSQL. As connection strings são
/// resolvidas via <see cref="ISecretProvider"/> — nunca lidas de <c>appsettings</c>.
/// </summary>
public sealed class DataConfiguration(ISecretProvider secretProvider) : IDataConfiguration, IAsyncDisposable
{
    /// <summary>Chave da connection string de leitura no cofre.</summary>
    public const string ChaveLeitura = "ConnectionStrings__ReadConnection";

    /// <summary>Chave da connection string de escrita no cofre.</summary>
    public const string ChaveEscrita = "ConnectionStrings__WriteConnection";

    private readonly SemaphoreSlim _semaforo = new(1, 1);

    private NpgsqlDataSource? _leitura;
    private NpgsqlDataSource? _escrita;

    public async Task<DbConnection> AbrirLeituraAsync(CancellationToken cancellationToken = default)
    {
        var dataSource = await ObterDataSourceAsync(
            () => _leitura,
            fonte => _leitura = fonte,
            ChaveLeitura,
            cancellationToken);

        return await dataSource.OpenConnectionAsync(cancellationToken);
    }

    public async Task<DbConnection> AbrirEscritaAsync(CancellationToken cancellationToken = default)
    {
        var dataSource = await ObterDataSourceAsync(
            () => _escrita,
            fonte => _escrita = fonte,
            ChaveEscrita,
            cancellationToken);

        return await dataSource.OpenConnectionAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_leitura is not null)
        {
            await _leitura.DisposeAsync();
        }

        if (_escrita is not null)
        {
            await _escrita.DisposeAsync();
        }

        _semaforo.Dispose();
    }

    private async Task<NpgsqlDataSource> ObterDataSourceAsync(
        Func<NpgsqlDataSource?> ler,
        Action<NpgsqlDataSource> gravar,
        string chaveSegredo,
        CancellationToken cancellationToken)
    {
        var existente = ler();

        if (existente is not null)
        {
            return existente;
        }

        await _semaforo.WaitAsync(cancellationToken);

        try
        {
            existente = ler();

            if (existente is not null)
            {
                return existente;
            }

            var connectionString = await secretProvider.GetAsync(chaveSegredo, cancellationToken);
            var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();

            gravar(dataSource);

            return dataSource;
        }
        finally
        {
            _semaforo.Release();
        }
    }
}
