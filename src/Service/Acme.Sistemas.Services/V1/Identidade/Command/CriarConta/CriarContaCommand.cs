using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Identidade.Command.CriarConta;

/// <param name="AceitouTermos">
/// Aceite explícito. Sem ele nenhum dado pessoal é tratado — nem a conta é criada.
/// </param>
public sealed record CriarContaCommand(
    string Email,
    string Senha,
    bool AceitouTermos,
    string VersaoDocumentos) : IRequest<CriarContaCommandResult>;
