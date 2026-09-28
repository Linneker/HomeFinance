namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.ExportacaoAdmin;

/// <summary>Motivo da habilitação ou desabilitação — obrigatório, para a trilha de auditoria.</summary>
public sealed record ExportacaoAdminRequest(string Motivo);
