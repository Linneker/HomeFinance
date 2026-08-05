using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations;

/// <summary>
/// Abertura controlada na RLS para a rotina diária de alertas de caixa.
/// </summary>
/// <remarks>
/// O alerta de caixa depende do tempo passar: uma data coberta ontem pode estar descoberta
/// hoje sem que ninguém tenha tocado no sistema. Reavaliá-lo exige uma rotina que rode sem
/// requisição — e, portanto, sem tenant selecionado. Sob a policy normal ela não enxergaria
/// tenant nenhum e nunca faria nada.
/// <para>
/// A saída é a mesma já usada no login: uma função <c>SECURITY DEFINER</c> estreita, que
/// devolve apenas os identificadores necessários para a rotina assumir cada tenant, um de cada
/// vez, com o contexto do dono. Nenhum dado financeiro passa por aqui — a leitura dos valores
/// continua sujeita à RLS normal, já dentro do contexto.
/// </para>
/// </remarks>
public sealed class V20260805010_RotinaDeAlertas : MigrationBase
{
    public override long Versao => 20260805010;

    public override string Descricao => "Função SECURITY DEFINER para a rotina diária de alertas";

    protected override string Sql => """
        CREATE OR REPLACE FUNCTION tenants_para_rotina()
        RETURNS TABLE (tenant_id uuid, usuario_id uuid)
        LANGUAGE sql
        SECURITY DEFINER
        STABLE
        SET search_path = public
        AS $$
            SELECT t.id, t.usuario_id
            FROM tenant t
            JOIN usuario u ON u.id = t.usuario_id
            WHERE u.excluido_em IS NULL
            ORDER BY t.id
        $$;

        REVOKE EXECUTE ON FUNCTION tenants_para_rotina() FROM PUBLIC;
        GRANT EXECUTE ON FUNCTION tenants_para_rotina() TO homefinance_app;
        """;
}
