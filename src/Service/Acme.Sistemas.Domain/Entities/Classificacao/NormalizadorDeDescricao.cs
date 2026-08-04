using System.Globalization;
using System.Text;

namespace Acme.Sistemas.Domain.Entities.Classificacao;

/// <summary>
/// Forma canônica da descrição, usada tanto para gravar o padrão quanto para casá-lo.
/// </summary>
/// <remarks>
/// A descrição que vem da fatura do cartão é ruidosa: "PAG*IFOOD", "pag*ifood  ",
/// "PAG*IFOOD RESTAURANTE". Sem uma forma canônica, a pessoa criaria uma regra por variação
/// da mesma loja e nenhuma delas casaria com a próxima.
/// <para>
/// A normalização acontece na <b>gravação</b>, não na consulta. Normalizar dentro do SELECT
/// impediria o uso do índice único por <c>(tenant_id, padrao)</c> e faria a varredura crescer
/// com o histórico do cliente.
/// </para>
/// </remarks>
public static class NormalizadorDeDescricao
{
    /// <summary>Maiúsculas, sem acento e sem espaço repetido.</summary>
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        // Decompõe para separar a letra do acento e descarta as marcas — assim "AÇAÍ" e
        // "ACAI" viram a mesma coisa, que é como a pessoa espera que se comportem.
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var construtor = new StringBuilder(decomposto.Length);
        var espacoPendente = false;

        foreach (var caractere in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(caractere))
            {
                espacoPendente = construtor.Length > 0;
                continue;
            }

            if (espacoPendente)
            {
                construtor.Append(' ');
                espacoPendente = false;
            }

            construtor.Append(char.ToUpperInvariant(caractere));
        }

        return construtor.ToString().Normalize(NormalizationForm.FormC);
    }
}
