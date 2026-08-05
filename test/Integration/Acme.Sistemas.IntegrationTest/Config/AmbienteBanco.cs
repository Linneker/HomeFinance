using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;
using Acme.Sistemas.Infrastructure.Secrets;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acme.Sistemas.IntegrationTest.Config;

/// <summary>
/// Provedor de segredos para teste: devolve a connection string do ambiente em vez de ir ao
/// OpenBao. O que está sob teste aqui é o isolamento do banco, não a leitura do cofre.
/// </summary>
public sealed class SecretProviderDeTeste(string connectionString) : ISecretProvider
{
    public Task<string> GetAsync(string chave, CancellationToken cancellationToken = default)
        => Task.FromResult(connectionString);
}

/// <summary>Contexto de requisição fixo, montado por cada teste.</summary>
public sealed class ContextoFixo : IContextoRequisicao
{
    public Guid? UsuarioId { get; set; }

    public Guid? TenantId { get; set; }

    public bool SomenteLeitura { get; set; } = true;
}

/// <summary>
/// Sobe o schema uma vez por execução da suíte e expõe o que os testes precisam.
/// </summary>
public sealed class AmbienteBanco : IAsyncLifetime
{
    public const string VariavelConnectionString = "HOMEFINANCE_TEST_DB";

    private const string PadraoConnectionString =
        "Host=localhost;Port=57432;Database=homefinance;Username=homefinance_owner;Password=troque_este_valor";

    private const string SenhaAppDeTeste = "app_teste_local";

    /// <summary>Conexão de dono: usada apenas para migrar e preparar o ambiente.</summary>
    public string ConnectionStringDono { get; } =
        Environment.GetEnvironmentVariable(VariavelConnectionString) ?? PadraoConnectionString;

    /// <summary>
    /// Conexão da aplicação, com o role sem BYPASSRLS. É a única que prova isolamento: o dono
    /// do banco é superusuário e ignora RLS mesmo com FORCE.
    /// </summary>
    public string ConnectionStringApp { get; private set; } = string.Empty;

    /// <summary>
    /// Um único <see cref="DataConfiguration"/> — e portanto um único pool — para a suíte
    /// inteira.
    /// </summary>
    /// <remarks>
    /// Cada instância abre o seu próprio pool e só o devolve ao ser descartada. Criando uma por
    /// operação, um teste que monte algumas dezenas de repositórios esgota os
    /// <c>max_connections</c> do PostgreSQL e derruba o teste seguinte com
    /// <c>53300: remaining connection slots are reserved…</c> — falha que aparece longe da causa
    /// e muda de vítima conforme a ordem de execução.
    /// <para>
    /// O contexto continua sendo por teste: quem o carrega é o <see cref="TransactionManager"/>,
    /// não o pool.
    /// </para>
    /// </remarks>
    private DataConfiguration? _dataConfiguration;

    public async Task InitializeAsync()
    {
        var dataConfiguration = new DataConfiguration(new SecretProviderDeTeste(ConnectionStringDono));

        var migrations = typeof(MigrationBase).Assembly
            .GetTypes()
            .Where(tipo => tipo is { IsAbstract: false, IsInterface: false }
                           && typeof(IMigration).IsAssignableFrom(tipo))
            .Select(tipo => (IMigration)Activator.CreateInstance(tipo)!)
            .ToList();

        var runner = new MigrationRunner(
            dataConfiguration, migrations, NullLogger<MigrationRunner>.Instance);

        // Idempotente: rodar de novo numa base já migrada não faz nada.
        await runner.ExecutarAsync(CancellationToken.None);

        // A senha do role da aplicação é definida fora da migration — em produção a partir do
        // OpenBao, aqui com um valor de teste.
        await using var conexao = await dataConfiguration.AbrirEscritaAsync(CancellationToken.None);
        await using var comando = conexao.CreateCommand();
        comando.CommandText = $"ALTER ROLE homefinance_app WITH PASSWORD '{SenhaAppDeTeste}'";
        await comando.ExecuteNonQueryAsync(CancellationToken.None);

        ConnectionStringApp = ConnectionStringDono
            .Replace("Username=homefinance_owner", "Username=homefinance_app")
            .Replace("Password=troque_este_valor", $"Password={SenhaAppDeTeste}");

        _dataConfiguration = new DataConfiguration(new SecretProviderDeTeste(ConnectionStringApp));
    }

    public async Task DisposeAsync()
    {
        if (_dataConfiguration is not null)
        {
            await _dataConfiguration.DisposeAsync();
        }
    }

    public TransactionManager CriarGerenciador(IContextoRequisicao contexto)
        => new(_dataConfiguration!, contexto);
}

[CollectionDefinition(nameof(BancoCollection))]
public sealed class BancoCollection : ICollectionFixture<AmbienteBanco>;
