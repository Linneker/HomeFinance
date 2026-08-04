using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Identidade.Command.RegistrarConsentimento;

public sealed record RegistrarConsentimentoCommand(
    Guid UsuarioId,
    string Documento,
    string Versao) : IRequest<RegistrarConsentimentoCommandResult>;
