namespace Acme.Sistemas.Repository.Repositories.V1.Assinatura.Query;

internal static class AssinaturaQuery
{
    private const string ColunasPlano =
        "id, codigo, nome, cota_compartilhamentos, preco_mensal, ativo, criado_em";

    private const string ColunasAssinatura = """
        id, usuario_id, plano_id, situacao, canal, periodo_fim, carencia_ate,
        comprovante_referencia, criado_em, atualizado_em
        """;

    public static string PlanosAtivos => $"""
        SELECT {ColunasPlano}
        FROM plano
        WHERE ativo = true
        ORDER BY preco_mensal
        """;

    /// <summary>
    /// Todos os planos, inclusive inativos. A policy de <c>plano</c> é <c>USING (true)</c>: o
    /// catálogo é igual para todo mundo, então a leitura completa não depende de contexto.
    /// </summary>
    public static string PlanosTodos => $"""
        SELECT {ColunasPlano}
        FROM plano
        ORDER BY preco_mensal
        """;

    /// <summary>Upsert do plano pela função <c>SECURITY DEFINER</c> — o app não escreve o catálogo direto.</summary>
    public const string SalvarPlano =
        "SELECT salvar_plano(@id, @codigo, @nome, @cota, @preco, @ativo, @criadoEm)";

    public static string PlanoPorCodigo => $"""
        SELECT {ColunasPlano}
        FROM plano
        WHERE codigo = @codigo
        """;

    public static string PlanoPorId => $"""
        SELECT {ColunasPlano}
        FROM plano
        WHERE id = @id
        """;

    /// <summary>
    /// A assinatura viva da conta. Cancelada fica de fora: ela é histórico, e devolvê-la faria
    /// a tela dizer "cancelada" para quem já recontratou.
    /// </summary>
    public static string VigenteDoUsuario => $"""
        SELECT {ColunasAssinatura}
        FROM assinatura
        WHERE usuario_id = @usuarioId
          AND situacao IN (1, 2, 3)
        """;

    /// <summary>Pela referência do provedor, via função <c>SECURITY DEFINER</c>.</summary>
    public const string PorReferencia =
        "SELECT * FROM assinatura_por_referencia(@canal, @referencia)";

    /// <summary>
    /// Situação do dono do tenant. Devolve só situação e datas — nada de plano, valor ou canal.
    /// </summary>
    public const string DoDonoDoTenant =
        "SELECT * FROM situacao_assinatura_do_tenant(@tenantId)";

    public const string ParaReconciliacao =
        "SELECT * FROM assinaturas_para_reconciliacao()";

    /// <summary>
    /// Compartilhamentos que o titular mantém de pé. Conta os vigentes, não os já concedidos
    /// alguma vez: revogar tem de devolver a cota, senão a pessoa perde o direito ao errar.
    /// </summary>
    public const string ContarCompartilhamentosVigentes = """
        SELECT count(*)
        FROM tenant_compartilhamento c
        JOIN tenant t ON t.id = c.tenant_id
        WHERE t.usuario_id = app_usuario_id()
          AND c.revogado_em IS NULL
        """;
}
