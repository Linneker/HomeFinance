using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Identidade.Command.ExcluirConta;

/// <summary>
/// Exclui a conta do titular.
/// </summary>
/// <remarks>
/// A exclusão é direito do titular sob a LGPD e por isso não depende de assinatura ativa nem
/// de onboarding concluído — nenhum Behavior de bloqueio alcança este Command.
/// <para>
/// O repositório revoga os compartilhamentos concedidos antes do DELETE, na mesma transação:
/// quem recebia acesso perde no mesmo instante, sem janela em que veria dado de uma conta que
/// já não existe.
/// </para>
/// </remarks>
public sealed class ExcluirContaCommandHandler(IUsuarioWriteRepository usuarios)
    : IRequestHandler<ExcluirContaCommand, ExcluirContaCommandResult>
{
    public async Task<ExcluirContaCommandResult> HandleAsync(
        ExcluirContaCommand request,
        CancellationToken cancellationToken)
    {
        var erros = ExcluirContaCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return ExcluirContaCommandResult.Recusada(erros[0]);
        }

        await usuarios.ExcluirContaAsync(request.UsuarioId, cancellationToken);

        return ExcluirContaCommandResult.Excluida();
    }
}
