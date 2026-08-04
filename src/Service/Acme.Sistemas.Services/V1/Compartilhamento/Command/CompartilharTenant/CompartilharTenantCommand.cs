using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;

/// <param name="TenantSelecionadoId">
/// Tenant ativo da requisição, resolvido pelo middleware — nunca informado pelo cliente. Serve
/// para recusar o repasse: quem está com o tenant de outra pessoa aberto e pede compartilhamento
/// recebe recusa, em vez de compartilhar o próprio tenant sem perceber.
/// </param>
/// <param name="ConsentimentoConfirmado">
/// Confirmação da tela que lista item a item o que ficará visível. Sem ela nada é concedido.
/// </param>
/// <param name="IncluiScr">
/// Categoria sensível: fica de fora por padrão e só entra por marcação explícita do dono.
/// </param>
public sealed record CompartilharTenantCommand(
    Guid DonoUsuarioId,
    Guid? TenantSelecionadoId,
    string EmailDestinatario,
    bool ConsentimentoConfirmado,
    bool IncluiScr) : IRequest<CompartilharTenantCommandResult>;
