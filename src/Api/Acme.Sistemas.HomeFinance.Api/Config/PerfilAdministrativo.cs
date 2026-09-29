using Acme.Sistemas.Core.Security;

namespace Acme.Sistemas.HomeFinance.Api.Config;

/// <summary>
/// Identidade administrativa, separada da do usuário final.
/// </summary>
/// <remarks>
/// A separação começa aqui: os endpoints de operação exigem a claim de perfil, que o token de
/// usuário final nunca carrega, e o login administrativo (<c>/api/v1/admin/sessoes</c>) emite o
/// token que a carrega. O nome e o valor da claim vêm do <see cref="GeradorDeToken"/> — fonte
/// única, para que quem emite e quem exige a claim não divirjam.
/// </remarks>
public static class PerfilAdministrativo
{
    public const string Claim = GeradorDeToken.ClaimPerfil;

    public const string Administrador = GeradorDeToken.PerfilAdministrador;

    /// <summary>Política de autorização exigida pelos endpoints de operação.</summary>
    public const string Politica = "administrador";
}
