using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.CopiarOrcamento;

/// <param name="Confirmado">
/// Aceite explícito de substituir o mês de destino. Sem ele, um destino que já tem valores faz
/// a operação parar e pedir confirmação em vez de apagar o que estava lá.
/// </param>
public sealed record CopiarOrcamentoCommand(
    Guid TenantId,
    DateOnly Origem,
    DateOnly Destino,
    bool Confirmado = false) : IRequest<CopiarOrcamentoCommandResult>;
