namespace Acme.Sistemas.Core.Security;

/// <summary>Política de senha forte, com os critérios devolvidos individualmente.</summary>
public static class PasswordHelper
{
    public const int ComprimentoMinimo = 10;

    /// <summary>
    /// Devolve os critérios não atendidos. Lista vazia significa senha válida.
    /// </summary>
    /// <remarks>
    /// Devolver a lista, e não um booleano, é o que permite à interface dizer o que falta em
    /// vez de repetir "senha inválida" até a pessoa desistir.
    /// </remarks>
    public static IReadOnlyList<string> CriteriosNaoAtendidos(string? senha)
    {
        var criterios = new List<string>();

        if (string.IsNullOrWhiteSpace(senha))
        {
            criterios.Add($"Informe uma senha com ao menos {ComprimentoMinimo} caracteres.");
            return criterios;
        }

        if (senha.Length < ComprimentoMinimo)
        {
            criterios.Add($"A senha deve ter ao menos {ComprimentoMinimo} caracteres.");
        }

        if (!senha.Any(char.IsUpper))
        {
            criterios.Add("A senha deve conter ao menos uma letra maiúscula.");
        }

        if (!senha.Any(char.IsLower))
        {
            criterios.Add("A senha deve conter ao menos uma letra minúscula.");
        }

        if (!senha.Any(char.IsDigit))
        {
            criterios.Add("A senha deve conter ao menos um número.");
        }

        if (senha.All(char.IsLetterOrDigit))
        {
            criterios.Add("A senha deve conter ao menos um caractere especial.");
        }

        return criterios;
    }

    public static bool EhForte(string? senha) => CriteriosNaoAtendidos(senha).Count == 0;
}
