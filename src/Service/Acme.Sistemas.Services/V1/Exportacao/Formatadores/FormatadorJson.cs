using System.Text;
using System.Text.Json;
using Acme.Sistemas.Domain.Entities.Exportacao;
using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Services.V1.Exportacao.Formatadores;

/// <summary>
/// Formato estruturado hierárquico — o conjunto inteiro, para consumo por outra ferramenta.
/// </summary>
/// <remarks>
/// Leva todas as seções, e não só os lançamentos: quem pega o JSON quer levar os próprios dados
/// para outro lugar, e categorias, orçamentos e renda fazem parte deles tanto quanto os
/// movimentos.
/// </remarks>
public sealed class FormatadorJson : IFormatadorExportacao
{
    private static readonly JsonSerializerOptions Opcoes = new() { WriteIndented = true };

    public FormatoExportacao Formato => FormatoExportacao.Json;

    public ArquivoGerado Gerar(DadosDoTenantParaExportacao dados)
    {
        var json = JsonSerializer.Serialize(dados, Opcoes);

        return new ArquivoGerado(Encoding.UTF8.GetBytes(json), "application/json", "json");
    }
}
