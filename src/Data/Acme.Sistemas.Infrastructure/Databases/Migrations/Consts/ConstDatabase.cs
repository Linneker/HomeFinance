namespace Acme.Sistemas.Infrastructure.Databases.Migrations.Consts;

public static class ConstDatabase
{
    /// <summary>Tabela de controle das migrations já aplicadas.</summary>
    public const string TabelaControleMigrations = "__migrations";

    /// <summary>
    /// Chave de advisory lock que serializa a execução do runner. Duas instâncias da Api
    /// subindo ao mesmo tempo tentariam migrar em paralelo; sem o lock, uma falha no meio.
    /// </summary>
    public const long ChaveLockMigration = 727_2026_01;

    /// <summary>Role da aplicação. Sem <c>BYPASSRLS</c> — é o que faz as policies valerem.</summary>
    public const string RoleAplicacao = "homefinance_app";

    /// <summary>Parâmetro de sessão com o tenant ativo da requisição.</summary>
    public const string ParametroTenant = "app.tenant_id";

    /// <summary>
    /// Parâmetro de sessão com o usuário autenticado. Protege as tabelas de conta, que são
    /// lidas antes de existir tenant selecionado.
    /// </summary>
    public const string ParametroUsuario = "app.usuario_id";

    /// <summary>Parâmetro de sessão que marca a requisição como somente leitura.</summary>
    public const string ParametroSomenteLeitura = "app.readonly";
}
