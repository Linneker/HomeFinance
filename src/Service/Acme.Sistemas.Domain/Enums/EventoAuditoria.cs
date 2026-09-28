namespace Acme.Sistemas.Domain.Enums;

/// <summary>
/// Evento registrado na trilha de auditoria da exportação de dados.
/// </summary>
/// <remarks>
/// A exportação toca no dado mais sensível do sistema — o financeiro do titular — por um caminho
/// que a operação abre a pedido. Auditar habilitação, solicitação e download é o que permite,
/// depois, responder "quem liberou, quando e por quê" sem depender da memória de ninguém.
/// </remarks>
public enum EventoAuditoria
{
    ExportacaoHabilitada = 1,

    ExportacaoDesabilitada = 2,

    ExportacaoSolicitada = 3,

    ExportacaoBaixada = 4,
}
