using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Core.Security;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Identidade.Command.Autenticar;

public sealed class AutenticarCommandHandler(
    IUsuarioWriteRepository usuarios,
    IGeradorDeToken gerador) : IRequestHandler<AutenticarCommand, AutenticarCommandResult>
{
    public const string CodigoCredencialInvalida = "credencial_invalida";

    /// <summary>
    /// Hash descartável usado quando o e-mail não existe.
    /// </summary>
    /// <remarks>
    /// Verificar contra ele mantém o custo da resposta parecido com o de uma conta real. Sem
    /// isso, "e-mail inexistente" responderia bem mais rápido que "senha errada", e o tempo de
    /// resposta viraria um oráculo para descobrir quais e-mails têm conta.
    /// </remarks>
    private static readonly string HashDeReferencia = Hash.GerarHashSenha(Guid.NewGuid().ToString());

    public async Task<AutenticarCommandResult> HandleAsync(
        AutenticarCommand request,
        CancellationToken cancellationToken)
    {
        var erros = AutenticarCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return AutenticarCommandResult.Recusado(erros[0]);
        }

        var credenciais = await usuarios.ObterCredenciaisAsync(request.Email, cancellationToken);

        if (credenciais is null)
        {
            _ = Hash.SenhaConfere(request.Senha, HashDeReferencia);
            return Recusar();
        }

        if (!Hash.SenhaConfere(request.Senha, credenciais.SenhaHash))
        {
            return Recusar();
        }

        var token = await gerador.EmitirAsync(
            credenciais.UsuarioId, credenciais.TenantPadraoId, cancellationToken);

        return AutenticarCommandResult.Autenticado(token.Token, token.ExpiraEm);
    }

    // Mensagem única para e-mail inexistente e senha errada: distinguir os dois entrega ao
    // atacante a lista de e-mails cadastrados.
    private static AutenticarCommandResult Recusar() => AutenticarCommandResult.Recusado(
        new Error(CodigoCredencialInvalida, "E-mail ou senha inválidos."));
}
