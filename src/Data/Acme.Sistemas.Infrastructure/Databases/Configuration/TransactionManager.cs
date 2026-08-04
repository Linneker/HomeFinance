using System.Data.Common;
using Acme.Sistemas.Infrastructure.Databases.Migrations.Consts;

namespace Acme.Sistemas.Infrastructure.Databases.Configuration;

/// <summary>
/// Abre conexão e transação já com o contexto de isolamento aplicado.
/// </summary>
/// <remarks>
/// O contexto é aplicado nas DUAS conexões, não só na de leitura. O <c>WriteRepository</c> executa
/// os SELECTs que decidem a escrita — verificação de unicidade, carga antes de alterar — e um
/// desses SELECTs sem contexto cairia na policy, voltaria vazio, e o Command concluiria que o
/// registro não existe. O resultado seria duplicata gravada: falha silenciosa, dado errado.
/// </remarks>
public sealed class TransactionManager(IDataConfiguration dataConfiguration, IContextoRequisicao contexto)
{
    public Task<EscopoTransacao> AbrirLeituraAsync(CancellationToken cancellationToken = default)
        => AbrirAsync(dataConfiguration.AbrirLeituraAsync, cancellationToken);

    public Task<EscopoTransacao> AbrirEscritaAsync(CancellationToken cancellationToken = default)
        => AbrirAsync(dataConfiguration.AbrirEscritaAsync, cancellationToken);

    private async Task<EscopoTransacao> AbrirAsync(
        Func<CancellationToken, Task<DbConnection>> abrir,
        CancellationToken cancellationToken)
    {
        var conexao = await abrir(cancellationToken);

        try
        {
            var transacao = await conexao.BeginTransactionAsync(cancellationToken);

            try
            {
                await AplicarContextoAsync(conexao, transacao, cancellationToken);
                return new EscopoTransacao(conexao, transacao);
            }
            catch
            {
                await transacao.DisposeAsync();
                throw;
            }
        }
        catch
        {
            await conexao.DisposeAsync();
            throw;
        }
    }

    private async Task AplicarContextoAsync(
        DbConnection conexao,
        DbTransaction transacao,
        CancellationToken cancellationToken)
    {
        await using var comando = conexao.CreateCommand();
        comando.Transaction = transacao;

        // SET LOCAL, nunca SET: SET persiste na conexão devolvida ao pool e vazaria o tenant
        // da requisição atual para a próxima que pegasse a mesma conexão.
        // set_config com parâmetro evita concatenar valor em SQL.
        comando.CommandText = $"""
            SELECT set_config('{ConstDatabase.ParametroUsuario}', @usuario, true),
                   set_config('{ConstDatabase.ParametroTenant}', @tenant, true),
                   set_config('{ConstDatabase.ParametroSomenteLeitura}', @somenteLeitura, true)
            """;

        AdicionarParametro(comando, "usuario", contexto.UsuarioId?.ToString() ?? string.Empty);
        AdicionarParametro(comando, "tenant", contexto.TenantId?.ToString() ?? string.Empty);
        AdicionarParametro(comando, "somenteLeitura", contexto.SomenteLeitura ? "true" : "false");

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AdicionarParametro(DbCommand comando, string nome, string valor)
    {
        var parametro = comando.CreateParameter();
        parametro.ParameterName = nome;
        parametro.Value = valor;
        comando.Parameters.Add(parametro);
    }
}

/// <summary>Conexão e transação com contexto aplicado. Descartar sempre com <c>await using</c>.</summary>
public sealed class EscopoTransacao(DbConnection conexao, DbTransaction transacao) : IAsyncDisposable
{
    public DbConnection Conexao { get; } = conexao;

    public DbTransaction Transacao { get; } = transacao;

    private bool _confirmada;

    public async Task ConfirmarAsync(CancellationToken cancellationToken = default)
    {
        await Transacao.CommitAsync(cancellationToken);
        _confirmada = true;
    }

    public async ValueTask DisposeAsync()
    {
        // Sem commit explícito, desfaz. Escrita esquecida não deve vazar para o banco só
        // porque alguém saiu do escopo sem confirmar.
        if (!_confirmada)
        {
            try
            {
                await Transacao.RollbackAsync();
            }
            catch
            {
                // A transação pode já ter sido encerrada pelo servidor; nada a fazer aqui.
            }
        }

        await Transacao.DisposeAsync();
        await Conexao.DisposeAsync();
    }
}
