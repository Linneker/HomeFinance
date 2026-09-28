using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Exportacao;

/// <summary>Pedido de exportação: o titular escolhe o formato.</summary>
public sealed record SolicitarExportacaoRequest(FormatoExportacao Formato);
