using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Services.V1.Exportacao.Command.SolicitarExportacao;

/// <summary>Pedido do titular para exportar os dados do próprio tenant.</summary>
/// <remarks>
/// Não é <c>IRecursoDerivado</c> de propósito: a habilitação prevalece sobre a restrição de
/// assinatura, então a solicitação não passa pelo <c>AssinaturaBehavior</c> e roda mesmo com a
/// assinatura suspensa. Também não é <c>IEscritaFinanceira</c> — exportar não é registrar dado
/// financeiro, e o onboarding pendente não pode barrar o titular de levar embora o que já tem.
/// </remarks>
/// <param name="SomenteLeitura">
/// Verdadeiro quando o tenant ativo foi recebido por compartilhamento. Exportar dado de terceiro
/// é recusado: quem exporta é o dono do tenant.
/// </param>
public sealed record SolicitarExportacaoCommand(
    Guid TenantId,
    Guid UsuarioSolicitanteId,
    bool SomenteLeitura,
    FormatoExportacao Formato) : IRequest<SolicitarExportacaoCommandResult>;
