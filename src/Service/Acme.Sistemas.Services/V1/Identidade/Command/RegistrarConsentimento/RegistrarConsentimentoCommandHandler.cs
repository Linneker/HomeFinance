using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Identidade.Command.RegistrarConsentimento;

/// <summary>
/// Registra o aceite de uma nova versão de documento.
/// </summary>
/// <remarks>
/// Grava sempre uma linha nova, nunca atualiza a anterior: o histórico de aceites é a prova de
/// o que a pessoa aceitou e quando. Sobrescrever apagaria justamente o que dá valor ao registro.
/// </remarks>
public sealed class RegistrarConsentimentoCommandHandler(
    IUsuarioWriteRepository usuarios,
    TimeProvider relogio)
    : IRequestHandler<RegistrarConsentimentoCommand, RegistrarConsentimentoCommandResult>
{
    public async Task<RegistrarConsentimentoCommandResult> HandleAsync(
        RegistrarConsentimentoCommand request,
        CancellationToken cancellationToken)
    {
        var erros = RegistrarConsentimentoCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return RegistrarConsentimentoCommandResult.Recusado(erros[0]);
        }

        var agora = relogio.GetUtcNow();

        await usuarios.RegistrarConsentimentoAsync(
            new ConsentimentoEntity
            {
                Id = Guid.CreateVersion7(),
                UsuarioId = request.UsuarioId,
                Documento = request.Documento,
                Versao = request.Versao,
                AceitoEm = agora,
                CriadoEm = agora,
            },
            cancellationToken);

        return RegistrarConsentimentoCommandResult.Registrado();
    }
}
