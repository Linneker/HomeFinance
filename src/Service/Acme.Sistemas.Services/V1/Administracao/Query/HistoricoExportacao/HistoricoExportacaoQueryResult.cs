using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Services.V1.Administracao.Query.HistoricoExportacao;

/// <summary>Uma entrada do histórico: o evento, quem o provocou, o motivo e quando.</summary>
public sealed record EntradaHistorico(
    Guid Id,
    EventoAuditoria Evento,
    Guid AutorId,
    string? Motivo,
    DateTimeOffset Quando);

public sealed record HistoricoExportacaoQueryResult(IReadOnlyList<EntradaHistorico> Entradas)
{
    public bool Sucesso => true;
}
