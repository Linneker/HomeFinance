using System.Data.Common;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

/// <summary>
/// Uma migration versionada. O SQL é escrito à mão: o schema desta aplicação depende de
/// recursos que nenhum builder fluente cobre bem — RLS com <c>FORCE</c>, policies por operação
/// e roles — e esconder isso atrás de abstração tornaria o isolamento menos auditável.
/// </summary>
public interface IMigration
{
    /// <summary>Ordem de execução. Único e crescente; nunca reutilizar um número já aplicado.</summary>
    long Versao { get; }

    /// <summary>Descrição curta, gravada no controle para leitura humana.</summary>
    string Descricao { get; }

    /// <summary>
    /// Aplica a migration. Recebe a conexão já dentro da transação do runner — não abrir outra,
    /// nem commitar aqui.
    /// </summary>
    Task AplicarAsync(DbConnection conexao, DbTransaction transacao, CancellationToken cancellationToken);
}
