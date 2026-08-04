using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Core.Security;
using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Identidade.Command.CriarConta;

/// <summary>
/// Cria a conta, provisiona o tenant individual e registra o consentimento — tudo num commit.
/// </summary>
/// <remarks>
/// Recebe o repositório de ESCRITA, nunca o de leitura: a verificação de e-mail existente
/// decide uma escrita e precisa enxergar o primário. Blueprint Acme 2.1.0.
/// </remarks>
public sealed class CriarContaCommandHandler(
    IUsuarioWriteRepository usuarios,
    TimeProvider relogio) : IRequestHandler<CriarContaCommand, CriarContaCommandResult>
{
    public const string CodigoEmailEmUso = "email_em_uso";

    public async Task<CriarContaCommandResult> HandleAsync(
        CriarContaCommand request,
        CancellationToken cancellationToken)
    {
        var erros = CriarContaCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return CriarContaCommandResult.Recusada([.. erros]);
        }

        if (await usuarios.ExisteEmailAsync(request.Email, cancellationToken))
        {
            // Mensagem sem detalhe sobre a conta existente: confirmar cadastro para quem não
            // é o titular é enumeração de usuários.
            return CriarContaCommandResult.Recusada(
                new Error(CodigoEmailEmUso, "Este e-mail já está em uso.", nameof(request.Email)));
        }

        var agora = relogio.GetUtcNow();

        var usuario = new UsuarioEntity
        {
            Id = Guid.CreateVersion7(),
            Email = request.Email.Trim(),
            SenhaHash = Hash.GerarHashSenha(request.Senha),
            CriadoEm = agora,
        };

        var tenant = new TenantEntity
        {
            Id = Guid.CreateVersion7(),
            UsuarioId = usuario.Id,
            CriadoEm = agora,
        };

        var consentimentos = DocumentoConsentimento.Obrigatorios
            .Select(documento => new ConsentimentoEntity
            {
                Id = Guid.CreateVersion7(),
                UsuarioId = usuario.Id,
                Documento = documento,
                Versao = request.VersaoDocumentos,
                AceitoEm = agora,
                CriadoEm = agora,
            })
            .ToList();

        await usuarios.CriarContaAsync(usuario, tenant, consentimentos, cancellationToken);

        return CriarContaCommandResult.Criada(usuario.Id, tenant.Id);
    }
}
