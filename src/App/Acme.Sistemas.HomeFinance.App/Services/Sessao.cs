namespace Acme.Sistemas.HomeFinance.App.Services;

/// <summary>
/// Guarda a sessão do usuário: o token de acesso e o instante de expiração.
/// </summary>
/// <remarks>
/// O token vai para o <see cref="SecureStorage"/> da plataforma (Keychain no iOS, Keystore no
/// Android, DPAPI no Windows) — nunca em arquivo claro nem em <c>Preferences</c>. É credencial de
/// acesso ao dado financeiro do titular; guardá-la em claro a exporia a qualquer app com leitura
/// do armazenamento.
/// </remarks>
public interface ISessao
{
    bool Autenticado { get; }

    Task<string?> TokenAsync();

    Task DefinirAsync(string token, DateTimeOffset expiraEm);

    Task LimparAsync();

    /// <summary>Restaura o estado do armazenamento seguro na inicialização.</summary>
    Task CarregarAsync();

    /// <summary>Disparado quando a sessão cai (logout ou expiração) para a navegação reagir.</summary>
    event EventHandler? Encerrada;
}

public sealed class Sessao : ISessao
{
    private const string ChaveToken = "homefinance.token";
    private const string ChaveExpira = "homefinance.token.expira";

    private string? _token;
    private DateTimeOffset _expiraEm;

    public bool Autenticado => _token is not null && _expiraEm > DateTimeOffset.UtcNow;

    public event EventHandler? Encerrada;

    public async Task CarregarAsync()
    {
        try
        {
            _token = await SecureStorage.Default.GetAsync(ChaveToken);
            var expira = await SecureStorage.Default.GetAsync(ChaveExpira);
            _expiraEm = expira is not null && DateTimeOffset.TryParse(expira, out var valor)
                ? valor
                : DateTimeOffset.MinValue;

            // Token já vencido não é sessão: descarta para a UI abrir o login limpo.
            if (_token is not null && _expiraEm <= DateTimeOffset.UtcNow)
            {
                await LimparAsync();
            }
        }
        catch
        {
            // Armazenamento seguro indisponível (ex.: sem provider no host de teste): sessão em memória.
            _token = null;
        }
    }

    public Task<string?> TokenAsync()
    {
        if (_token is not null && _expiraEm <= DateTimeOffset.UtcNow)
        {
            // Expirou desde o último uso: encerra e devolve nulo, sem chamar a Api com token morto.
            _token = null;
            Encerrada?.Invoke(this, EventArgs.Empty);
        }

        return Task.FromResult(_token);
    }

    public async Task DefinirAsync(string token, DateTimeOffset expiraEm)
    {
        _token = token;
        _expiraEm = expiraEm;
        try
        {
            await SecureStorage.Default.SetAsync(ChaveToken, token);
            await SecureStorage.Default.SetAsync(ChaveExpira, expiraEm.ToString("O"));
        }
        catch
        {
            // Sem armazenamento seguro, a sessão vale só enquanto o app estiver aberto.
        }
    }

    public Task LimparAsync()
    {
        _token = null;
        _expiraEm = DateTimeOffset.MinValue;
        try
        {
            SecureStorage.Default.Remove(ChaveToken);
            SecureStorage.Default.Remove(ChaveExpira);
        }
        catch
        {
            /* nada a fazer */
        }

        Encerrada?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}
