namespace Acme.Sistemas.Domain.Enums;

/// <summary>
/// Formato do arquivo de exportação, escolhido pelo titular na solicitação.
/// </summary>
/// <remarks>
/// A spec exige ao menos um formato de leitura humana e um de dados estruturados. São eixos
/// diferentes: o titular que quer conferir o próprio histórico abre o relatório; quem vai levar
/// os dados para outra ferramenta pega o CSV ou o JSON. Oferecer só um dos eixos serviria mal
/// metade dos motivos pelos quais alguém pede os próprios dados.
/// </remarks>
public enum FormatoExportacao
{
    /// <summary>Dados estruturados em linhas e colunas — abre em qualquer planilha.</summary>
    Csv = 1,

    /// <summary>Dados estruturados hierárquicos — para consumo por outra ferramenta.</summary>
    Json = 2,

    /// <summary>Relatório de leitura humana, com os lançamentos organizados para ler.</summary>
    Texto = 3,
}
