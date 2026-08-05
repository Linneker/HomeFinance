using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Alerta;

public sealed record AlertaItemResponse(
    Guid Id,
    TipoAlerta Tipo,
    string Mensagem,
    DateTimeOffset CriadoEm);

public sealed record AlertasResponse(IReadOnlyList<AlertaItemResponse> Alertas);
