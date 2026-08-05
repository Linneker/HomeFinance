using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Alertas.Query.AlertasAbertos;

/// <param name="SomenteLeitura">
/// Verdadeiro quando o tenant ativo foi recebido por compartilhamento. Alerta é do dono: quem
/// só lê não recebe nenhum.
/// </param>
public sealed record AlertasAbertosQuery(Guid TenantId, bool SomenteLeitura)
    : IRequest<AlertasAbertosQueryResult>;
