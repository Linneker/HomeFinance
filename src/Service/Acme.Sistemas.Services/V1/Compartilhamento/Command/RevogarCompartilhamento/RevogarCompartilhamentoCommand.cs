using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Command.RevogarCompartilhamento;

/// <param name="TenantSelecionadoId">
/// Tenant ativo da requisição, resolvido pelo middleware. Recusa a revogação pedida com o
/// tenant de outra pessoa aberto — que, sem isto, encerraria um acesso concedido pelo próprio
/// solicitante em vez de dizer que ele não pode revogar ali.
/// </param>
public sealed record RevogarCompartilhamentoCommand(
    Guid DonoUsuarioId,
    Guid? TenantSelecionadoId,
    Guid UsuarioDestinatarioId) : IRequest<RevogarCompartilhamentoCommandResult>;
