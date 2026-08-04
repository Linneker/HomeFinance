using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Acme.Sistemas.Infrastructure.Secrets;

/// <summary>
/// Lê segredos do OpenBao pelo engine KV v2. Os valores são mantidos em memória por um período
/// curto — ler o cofre a cada abertura de conexão inviabilizaria o throughput da aplicação.
/// </summary>
public sealed class OpenBaoSecretProvider : ISecretProvider
{
    private readonly HttpClient _httpClient;
    private readonly OpenBaoOptions _options;
    private readonly SemaphoreSlim _semaforo = new(1, 1);
    private readonly TimeProvider _timeProvider;

    private Dictionary<string, string>? _segredos;
    private DateTimeOffset _expiraEm = DateTimeOffset.MinValue;

    public OpenBaoSecretProvider(
        HttpClient httpClient,
        IOptions<OpenBaoOptions> options,
        TimeProvider? timeProvider = null)
    {
        _options = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _httpClient = httpClient;

        if (string.IsNullOrWhiteSpace(_options.Endereco))
        {
            throw new InvalidOperationException(
                "OpenBao:Endereco não configurado. A aplicação não sobe sem origem de segredos.");
        }

        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            throw new InvalidOperationException(
                "Token do OpenBao ausente. Ele vem do ambiente, nunca de appsettings.");
        }

        _httpClient.BaseAddress = new Uri(_options.Endereco.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Add("X-Vault-Token", _options.Token);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> GetAsync(string chave, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chave);

        var segredos = await ObterSegredosAsync(cancellationToken);

        if (!segredos.TryGetValue(chave, out var valor) || string.IsNullOrWhiteSpace(valor))
        {
            throw new KeyNotFoundException(
                $"Segredo '{chave}' não encontrado em {_options.Mount}/{_options.Caminho}.");
        }

        return valor;
    }

    private async Task<Dictionary<string, string>> ObterSegredosAsync(CancellationToken cancellationToken)
    {
        if (_segredos is not null && _timeProvider.GetUtcNow() < _expiraEm)
        {
            return _segredos;
        }

        await _semaforo.WaitAsync(cancellationToken);

        try
        {
            // Outra thread pode ter recarregado enquanto esta esperava o semáforo.
            if (_segredos is not null && _timeProvider.GetUtcNow() < _expiraEm)
            {
                return _segredos;
            }

            var rota = $"v1/{_options.Mount}/data/{_options.Caminho}";
            using var resposta = await _httpClient.GetAsync(rota, cancellationToken);

            if (!resposta.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Falha ao ler segredos do OpenBao em '{rota}': {(int)resposta.StatusCode} {resposta.ReasonPhrase}.");
            }

            await using var conteudo = await resposta.Content.ReadAsStreamAsync(cancellationToken);
            using var documento = await JsonDocument.ParseAsync(conteudo, cancellationToken: cancellationToken);

            var dados = documento.RootElement.GetProperty("data").GetProperty("data");

            var lidos = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var propriedade in dados.EnumerateObject())
            {
                if (propriedade.Value.ValueKind == JsonValueKind.String)
                {
                    lidos[propriedade.Name] = propriedade.Value.GetString() ?? string.Empty;
                }
            }

            _segredos = lidos;
            _expiraEm = _timeProvider.GetUtcNow().Add(_options.DuracaoCache);

            return _segredos;
        }
        finally
        {
            _semaforo.Release();
        }
    }
}
