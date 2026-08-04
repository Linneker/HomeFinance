using System.Data.Common;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Infrastructure.Databases.Migrations.Consts;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

/// <summary>
/// Aplica migrations pendentes em ordem de versão, uma transação por migration.
/// </summary>
/// <remarks>
/// Idempotente por construção: cada migration aplicada é registrada em
/// <see cref="ConstDatabase.TabelaControleMigrations"/> e nunca reexecuta. Um advisory lock
/// serializa instâncias concorrentes — duas réplicas da Api subindo juntas tentariam migrar ao
/// mesmo tempo e uma quebraria no meio.
/// </remarks>
public sealed class MigrationRunner(
    IDataConfiguration dataConfiguration,
    IEnumerable<IMigration> migrations,
    ILogger<MigrationRunner> logger)
{
    public async Task<int> ExecutarAsync(CancellationToken cancellationToken = default)
    {
        // Migration é DDL: sempre pelo primário, nunca pela réplica.
        await using var conexao = await dataConfiguration.AbrirEscritaAsync(cancellationToken);

        await ObterLockAsync(conexao, cancellationToken);

        try
        {
            await GarantirTabelaControleAsync(conexao, cancellationToken);

            var aplicadas = await ObterVersoesAplicadasAsync(conexao, cancellationToken);

            var pendentes = migrations
                .OrderBy(migration => migration.Versao)
                .Where(migration => !aplicadas.Contains(migration.Versao))
                .ToList();

            GarantirVersoesUnicas();

            if (pendentes.Count == 0)
            {
                logger.LogInformation("Nenhuma migration pendente.");
                return 0;
            }

            foreach (var migration in pendentes)
            {
                await AplicarAsync(conexao, migration, cancellationToken);
            }

            logger.LogInformation("{Total} migration(s) aplicada(s).", pendentes.Count);
            return pendentes.Count;
        }
        finally
        {
            await LiberarLockAsync(conexao, cancellationToken);
        }
    }

    private void GarantirVersoesUnicas()
    {
        var duplicadas = migrations
            .GroupBy(migration => migration.Versao)
            .Where(grupo => grupo.Count() > 1)
            .Select(grupo => grupo.Key)
            .ToArray();

        if (duplicadas.Length > 0)
        {
            // Duas migrations com a mesma versão fariam uma delas nunca rodar, e o defeito só
            // apareceria em produção como coluna faltando.
            throw new InvalidOperationException(
                $"Versões de migration duplicadas: {string.Join(", ", duplicadas)}.");
        }
    }

    private async Task AplicarAsync(
        DbConnection conexao,
        IMigration migration,
        CancellationToken cancellationToken)
    {
        await using var transacao = await conexao.BeginTransactionAsync(cancellationToken);

        try
        {
            logger.LogInformation(
                "Aplicando migration {Versao} — {Descricao}.", migration.Versao, migration.Descricao);

            await migration.AplicarAsync(conexao, transacao, cancellationToken);

            await using (var registro = conexao.CreateCommand())
            {
                registro.Transaction = transacao;
                registro.CommandText = $"""
                    INSERT INTO {ConstDatabase.TabelaControleMigrations} (versao, descricao, aplicada_em)
                    VALUES (@versao, @descricao, now() AT TIME ZONE 'UTC')
                    """;

                AdicionarParametro(registro, "versao", migration.Versao);
                AdicionarParametro(registro, "descricao", migration.Descricao);

                await registro.ExecuteNonQueryAsync(cancellationToken);
            }

            await transacao.CommitAsync(cancellationToken);
        }
        catch (Exception excecao)
        {
            await transacao.RollbackAsync(cancellationToken);

            logger.LogError(
                excecao, "Falha na migration {Versao} — {Descricao}.", migration.Versao, migration.Descricao);

            // Parar na primeira falha: seguir adiante aplicaria migrations sobre um schema
            // em estado desconhecido.
            throw;
        }
    }

    private static async Task GarantirTabelaControleAsync(
        DbConnection conexao,
        CancellationToken cancellationToken)
    {
        await using var comando = conexao.CreateCommand();
        comando.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {ConstDatabase.TabelaControleMigrations} (
                versao      bigint      PRIMARY KEY,
                descricao   text        NOT NULL,
                aplicada_em timestamptz NOT NULL
            )
            """;

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<HashSet<long>> ObterVersoesAplicadasAsync(
        DbConnection conexao,
        CancellationToken cancellationToken)
    {
        await using var comando = conexao.CreateCommand();
        comando.CommandText = $"SELECT versao FROM {ConstDatabase.TabelaControleMigrations}";

        var versoes = new HashSet<long>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            versoes.Add(leitor.GetInt64(0));
        }

        return versoes;
    }

    private static async Task ObterLockAsync(DbConnection conexao, CancellationToken cancellationToken)
    {
        await using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT pg_advisory_lock(@chave)";
        AdicionarParametro(comando, "chave", ConstDatabase.ChaveLockMigration);

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task LiberarLockAsync(DbConnection conexao, CancellationToken cancellationToken)
    {
        await using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT pg_advisory_unlock(@chave)";
        AdicionarParametro(comando, "chave", ConstDatabase.ChaveLockMigration);

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AdicionarParametro(DbCommand comando, string nome, object valor)
    {
        var parametro = comando.CreateParameter();
        parametro.ParameterName = nome;
        parametro.Value = valor;
        comando.Parameters.Add(parametro);
    }
}
