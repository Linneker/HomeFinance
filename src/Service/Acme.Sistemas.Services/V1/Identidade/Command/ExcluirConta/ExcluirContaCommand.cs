using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Identidade.Command.ExcluirConta;

/// <param name="ConfirmacaoDoTitular">
/// Confirmação explícita. A exclusão é irreversível e cascateia para todo o dado financeiro —
/// não pode acontecer por clique acidental.
/// </param>
public sealed record ExcluirContaCommand(
    Guid UsuarioId,
    bool ConfirmacaoDoTitular) : IRequest<ExcluirContaCommandResult>;
