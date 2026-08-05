namespace Acme.Sistemas.Repository.Repositories.V1.Assinatura.Query;

internal static class AssinaturaCommand
{
    public const string Criar = """
        INSERT INTO assinatura
            (id, usuario_id, plano_id, situacao, canal, periodo_fim, carencia_ate,
             comprovante_referencia, criado_em)
        VALUES
            (@id, @usuarioId, @planoId, @situacao, @canal, @periodoFim, @carenciaAte,
             @comprovanteReferencia, @criadoEm)
        """;

    /// <summary>
    /// Aplica a situação pela função <c>SECURITY DEFINER</c>.
    /// </summary>
    /// <remarks>
    /// A notificação do provedor chega sem usuário autenticado; um UPDATE direto cairia na
    /// policy e não alteraria linha nenhuma — em silêncio, que é o pior desfecho possível para
    /// uma renovação confirmada.
    /// </remarks>
    public const string AplicarSituacao = """
        SELECT aplicar_situacao_assinatura(
            @id, @situacao, @periodoFim, @carenciaAte, @atualizadoEm)
        """;

    public const string RegistrarTransacao = """
        INSERT INTO transacao_pagamento
            (id, assinatura_id, usuario_id, canal, identificador_provedor, valor, situacao, criado_em)
        VALUES
            (@id, @assinaturaId, @usuarioId, @canal, @identificadorProvedor, @valor, @situacao, @criadoEm)
        ON CONFLICT (canal, identificador_provedor) DO NOTHING
        """;

    /// <summary>
    /// Registra a notificação. O <c>DO NOTHING</c> sobre o índice único é a idempotência: o
    /// provedor reenvia até receber confirmação, e processar um estorno duas vezes suspenderia
    /// uma assinatura já regularizada no meio tempo.
    /// </summary>
    public const string RegistrarNotificacao = """
        INSERT INTO notificacao_provedor (id, canal, identificador, corpo, recebida_em)
        VALUES (@id, @canal, @identificador, @corpo, @recebidaEm)
        ON CONFLICT (canal, identificador) DO NOTHING
        """;
}
