using System.Globalization;
using System.Text;
using Acme.Sistemas.Domain.Entities.Exportacao;
using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Services.V1.Exportacao.Formatadores;

/// <summary>
/// Formato estruturado tabular — os lançamentos em linhas e colunas, abre em qualquer planilha.
/// </summary>
/// <remarks>
/// Datas em ISO e decimais com ponto, cultura invariante: um CSV que muda de forma conforme o
/// idioma da máquina que o gerou é um CSV que a próxima ferramenta lê errado. O separador é a
/// vírgula, e todo campo textual vai entre aspas com escape — descrição de lançamento carrega
/// vírgula o tempo todo.
/// </remarks>
public sealed class FormatadorCsv : IFormatadorExportacao
{
    public FormatoExportacao Formato => FormatoExportacao.Csv;

    public ArquivoGerado Gerar(DadosDoTenantParaExportacao dados)
    {
        var texto = new StringBuilder();
        texto.AppendLine("competencia,caixa,descricao,valor,tipo,categoria");

        foreach (var lancamento in dados.Lancamentos)
        {
            texto.Append(lancamento.Competencia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',')
                 .Append(lancamento.Caixa.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',')
                 .Append(Campo(lancamento.Descricao)).Append(',')
                 .Append(lancamento.Valor.ToString(CultureInfo.InvariantCulture)).Append(',')
                 .Append(Campo(lancamento.Tipo)).Append(',')
                 .Append(Campo(lancamento.Categoria ?? string.Empty))
                 .Append('\n');
        }

        // UTF-8 com BOM: sem ele, o Excel em pt-BR abre acento quebrado, e o arquivo é do titular
        // ler, não só de uma ferramenta processar.
        return new ArquivoGerado(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(texto.ToString())).ToArray(),
            "text/csv", "csv");
    }

    private static string Campo(string valor) => $"\"{valor.Replace("\"", "\"\"")}\"";
}
