using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations;

/// <summary>
/// Concede ao role da aplicação acesso às tabelas já criadas.
/// </summary>
/// <remarks>
/// <c>ALTER DEFAULT PRIVILEGES</c> só alcança objetos criados depois dele e pelo mesmo role.
/// Esta migration fecha a lacuna das tabelas existentes e roda por último, quando todas já
/// existem. Migrations futuras que criem tabela devem repetir o <c>GRANT</c>.
/// </remarks>
public sealed class V20260803007_PrivilegiosDaAplicacao : MigrationBase
{
    public override long Versao => 20260803007;

    public override string Descricao => "Privilégios do role da aplicação sobre as tabelas";

    protected override string Sql => """
        GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO homefinance_app;
        GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO homefinance_app;

        -- O controle de migrations é só leitura para a aplicação: quem migra é o processo de
        -- deploy, conectado com outro role.
        REVOKE INSERT, UPDATE, DELETE ON __migrations FROM homefinance_app;

        -- Catálogo do onboarding é configuração do produto: a aplicação lê, não escreve.
        REVOKE INSERT, UPDATE, DELETE ON onboarding_requisito FROM homefinance_app;
        """;
}
