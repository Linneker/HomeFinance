using System.Data.Common;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

/// <summary>Base das migrations: executa o SQL na transação aberta pelo runner.</summary>
public abstract class MigrationBase : IMigration
{
    public abstract long Versao { get; }

    public abstract string Descricao { get; }

    protected abstract string Sql { get; }

    public async Task AplicarAsync(
        DbConnection conexao,
        DbTransaction transacao,
        CancellationToken cancellationToken)
    {
        await using var comando = conexao.CreateCommand();
        comando.Transaction = transacao;
        comando.CommandText = Sql;

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }
}
