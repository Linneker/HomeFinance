namespace Acme.Sistemas.HomeFinance.Api.Config;

/// <summary>
/// Identidade administrativa, separada da do usuário final.
/// </summary>
/// <remarks>
/// A habilitação da exportação existe só no portal administrativo, e a separação começa aqui: os
/// endpoints de operação exigem a claim de perfil, que o token de usuário final nunca carrega. A
/// emissão desse token — o login de administrador — é da fase de back-office; nesta fase fica a
/// trava que garante que nenhum usuário final alcança esses endpoints.
/// </remarks>
public static class PerfilAdministrativo
{
    public const string Claim = "perfil";

    public const string Administrador = "administrador";

    /// <summary>Política de autorização exigida pelos endpoints de operação.</summary>
    public const string Politica = "administrador";
}
