using System.Data.Common;

namespace Acme.Sistemas.Repository.Helper;

/// <summary>
/// Auxiliares de leitura e parametrização. Sem ORM, materialização e parâmetros são escritos
/// à mão — estes métodos evitam repetir o mesmo bloco em cada repositório.
/// </summary>
internal static class ConvertExtentions
{
    public static DbCommand Comando(this EscopoSql escopo, string sql)
    {
        var comando = escopo.Conexao.CreateCommand();
        comando.Transaction = escopo.Transacao;
        comando.CommandText = sql;
        return comando;
    }

    /// <summary>
    /// Sempre parametrizar. Concatenar valor em SQL é injeção — OWASP é regra do blueprint.
    /// </summary>
    public static DbCommand Com(this DbCommand comando, string nome, object? valor)
    {
        var parametro = comando.CreateParameter();
        parametro.ParameterName = nome;
        parametro.Value = valor ?? DBNull.Value;
        comando.Parameters.Add(parametro);
        return comando;
    }

    public static string TextoObrigatorio(this DbDataReader leitor, string coluna)
        => leitor.GetString(leitor.GetOrdinal(coluna));

    public static Guid Identificador(this DbDataReader leitor, string coluna)
        => leitor.GetGuid(leitor.GetOrdinal(coluna));

    public static Guid? IdentificadorOpcional(this DbDataReader leitor, string coluna)
    {
        var ordinal = leitor.GetOrdinal(coluna);
        return leitor.IsDBNull(ordinal) ? null : leitor.GetGuid(ordinal);
    }

    /// <summary>
    /// Coluna <c>date</c>. Lida como <see cref="DateOnly"/> e não como <c>DateTime</c>:
    /// competência e caixa são dias, e carregar uma hora zero junto convida a comparação por
    /// fuso onde não existe hora nenhuma.
    /// </summary>
    public static DateOnly Data(this DbDataReader leitor, string coluna)
        => leitor.GetFieldValue<DateOnly>(leitor.GetOrdinal(coluna));

    public static int? InteiroOpcional(this DbDataReader leitor, string coluna)
    {
        var ordinal = leitor.GetOrdinal(coluna);
        return leitor.IsDBNull(ordinal) ? null : leitor.GetInt32(ordinal);
    }

    public static DateTimeOffset DataHora(this DbDataReader leitor, string coluna)
    {
        var ordinal = leitor.GetOrdinal(coluna);
        return new DateTimeOffset(DateTime.SpecifyKind(leitor.GetDateTime(ordinal), DateTimeKind.Utc));
    }

    public static DateTimeOffset? DataHoraOpcional(this DbDataReader leitor, string coluna)
    {
        var ordinal = leitor.GetOrdinal(coluna);
        return leitor.IsDBNull(ordinal)
            ? null
            : new DateTimeOffset(DateTime.SpecifyKind(leitor.GetDateTime(ordinal), DateTimeKind.Utc));
    }
}

/// <summary>Par conexão/transação já com contexto de tenant aplicado.</summary>
internal readonly record struct EscopoSql(DbConnection Conexao, DbTransaction Transacao);
