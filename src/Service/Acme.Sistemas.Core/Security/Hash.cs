using System.Security.Cryptography;

namespace Acme.Sistemas.Core.Security;

/// <summary>
/// Hash de senha com PBKDF2-HMAC-SHA512 e sal por usuário.
/// </summary>
/// <remarks>
/// SHA puro não serve para senha: é rápido demais e viabiliza ataque de força bruta em GPU.
/// PBKDF2 mantém a família SHA exigida pelo blueprint e acrescenta o custo de derivação que
/// torna o ataque inviável. A comparação é em tempo constante — comparar hash com <c>==</c>
/// vaza informação por tempo de resposta.
/// </remarks>
public static class Hash
{
    private const int TamanhoSal = 16;
    private const int TamanhoChave = 64;
    private const int Iteracoes = 210_000;

    public static string GerarHashSenha(string senha)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(senha);

        var sal = RandomNumberGenerator.GetBytes(TamanhoSal);
        var chave = Rfc2898DeriveBytes.Pbkdf2(senha, sal, Iteracoes, HashAlgorithmName.SHA512, TamanhoChave);

        // O formato carrega os parâmetros junto: aumentar as iterações no futuro não invalida
        // os hashes já gravados.
        return $"pbkdf2-sha512${Iteracoes}${Convert.ToBase64String(sal)}${Convert.ToBase64String(chave)}";
    }

    public static bool SenhaConfere(string senha, string hashArmazenado)
    {
        if (string.IsNullOrWhiteSpace(senha) || string.IsNullOrWhiteSpace(hashArmazenado))
        {
            return false;
        }

        var partes = hashArmazenado.Split('$');

        if (partes.Length != 4 || partes[0] != "pbkdf2-sha512" || !int.TryParse(partes[1], out var iteracoes))
        {
            return false;
        }

        try
        {
            var sal = Convert.FromBase64String(partes[2]);
            var esperado = Convert.FromBase64String(partes[3]);
            var calculado = Rfc2898DeriveBytes.Pbkdf2(
                senha, sal, iteracoes, HashAlgorithmName.SHA512, esperado.Length);

            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
