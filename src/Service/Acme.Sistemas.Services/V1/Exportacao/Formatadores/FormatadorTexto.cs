using System.Globalization;
using System.Text;
using Acme.Sistemas.Domain.Entities.Exportacao;
using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Services.V1.Exportacao.Formatadores;

/// <summary>
/// Formato de leitura humana — um relatório organizado em seções, para o titular conferir.
/// </summary>
/// <remarks>
/// pt-BR: valores em reais, datas no formato do país. É o formato de quem pediu os próprios dados
/// para ler, não para reimportar — daí o cuidado com a apresentação que o CSV e o JSON não têm.
/// </remarks>
public sealed class FormatadorTexto : IFormatadorExportacao
{
    private static readonly CultureInfo Brasil = CultureInfo.GetCultureInfo("pt-BR");

    public FormatoExportacao Formato => FormatoExportacao.Texto;

    public ArquivoGerado Gerar(DadosDoTenantParaExportacao dados)
    {
        var texto = new StringBuilder();
        texto.AppendLine("EXPORTAÇÃO DE DADOS — HomeFinance").AppendLine(new string('=', 40)).AppendLine();

        texto.AppendLine($"LANÇAMENTOS ({dados.Lancamentos.Count})").AppendLine(new string('-', 40));
        foreach (var l in dados.Lancamentos)
        {
            texto.AppendLine(
                $"{l.Competencia:dd/MM/yyyy} | caixa {l.Caixa:dd/MM/yyyy} | {Moeda(l.Valor)} | {l.Tipo} | {l.Categoria ?? "sem categoria"} | {l.Descricao}");
        }

        texto.AppendLine().AppendLine($"CATEGORIAS ({dados.Categorias.Count})").AppendLine(new string('-', 40));
        foreach (var c in dados.Categorias)
        {
            texto.AppendLine($"- {c.Nome}");
        }

        texto.AppendLine().AppendLine($"ORÇAMENTO POR MÊS ({dados.Orcamentos.Count})").AppendLine(new string('-', 40));
        foreach (var o in dados.Orcamentos)
        {
            texto.AppendLine($"{o.Competencia:MM/yyyy} | {o.Categoria} | {Moeda(o.ValorPlanejado)}");
        }

        texto.AppendLine().AppendLine($"FONTES DE RENDA ({dados.FontesRenda.Count})").AppendLine(new string('-', 40));
        foreach (var f in dados.FontesRenda)
        {
            var ate = f.VigenteAte is null ? "em aberto" : f.VigenteAte.Value.ToString("dd/MM/yyyy", Brasil);
            texto.AppendLine($"{f.Descricao} | {Moeda(f.Valor)} | de {f.VigenteDe:dd/MM/yyyy} até {ate}");
        }

        return new ArquivoGerado(Encoding.UTF8.GetBytes(texto.ToString()), "text/plain; charset=utf-8", "txt");
    }

    private static string Moeda(decimal valor) => valor.ToString("C", Brasil);
}
