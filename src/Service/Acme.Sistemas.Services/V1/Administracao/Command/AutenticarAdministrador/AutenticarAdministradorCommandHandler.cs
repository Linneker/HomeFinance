using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Core.Security;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Administracao.Command.AutenticarAdministrador;

/// <summary>
/// Autentica o administrador contra a identidade administrativa e emite um token de perfil.
/// </summary>
/// <remarks>
/// A credencial é buscada só entre administradores: uma senha de usuário final não encontra nada
/// aqui e é recusada, e o inverso vale no login do usuário final. É o que separa os dois lados —
/// não é uma checagem de aplicação sobre um mesmo repositório, são repositórios distintos sobre
/// tabelas distintas. Mensagem única para e-mail inexistente e senha errada, e verificação
/// contra um hash de referência quando o e-mail não existe, para o tempo de resposta não virar
/// oráculo de quais administradores existem.
/// </remarks>
public sealed class AutenticarAdministradorCommandHandler(
    IAdministradorRepository administradores,
    IGeradorDeToken gerador)
    : IRequestHandler<AutenticarAdministradorCommand, AutenticarAdministradorCommandResult>
{
    public const string CodigoCredencialInvalida = "credencial_invalida";

    private static readonly string HashDeReferencia = Hash.GerarHashSenha(Guid.NewGuid().ToString());

    public async Task<AutenticarAdministradorCommandResult> HandleAsync(
        AutenticarAdministradorCommand request,
        CancellationToken cancellationToken)
    {
        var erros = AutenticarAdministradorCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return AutenticarAdministradorCommandResult.Recusado(erros[0]);
        }

        var credenciais = await administradores.ObterCredenciaisAsync(request.Email, cancellationToken);

        if (credenciais is null)
        {
            _ = Hash.SenhaConfere(request.Senha, HashDeReferencia);
            return Recusar();
        }

        if (!Hash.SenhaConfere(request.Senha, credenciais.SenhaHash))
        {
            return Recusar();
        }

        var token = await gerador.EmitirAdministradorAsync(credenciais.AdministradorId, cancellationToken);

        return AutenticarAdministradorCommandResult.Autenticado(token.Token, token.ExpiraEm);
    }

    private static AutenticarAdministradorCommandResult Recusar() => AutenticarAdministradorCommandResult.Recusado(
        new Error(CodigoCredencialInvalida, "E-mail ou senha inválidos."));
}
