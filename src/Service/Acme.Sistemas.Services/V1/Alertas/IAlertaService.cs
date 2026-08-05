namespace Acme.Sistemas.Services.V1.Alertas;

/// <param name="Emitidos">Condições que passaram a valer agora.</param>
/// <param name="Encerrados">Condições que deixaram de valer.</param>
/// <param name="JaConhecidos">
/// Condições que continuam valendo e já tinham alerta — aberto ou dispensado. Contadas, e não
/// reemitidas: é o que impede o mesmo aviso de voltar a cada lançamento gravado.
/// </param>
public sealed record ResultadoDaAvaliacao(int Emitidos, int Encerrados, int JaConhecidos)
{
    public static readonly ResultadoDaAvaliacao Nenhum = new(0, 0, 0);
}

/// <summary>
/// Avalia as duas condições de alerta e mantém o ciclo de vida delas.
/// </summary>
/// <remarks>
/// É serviço compartilhado, e não a própria etapa do pipeline: os gatilhos são quatro Commands
/// distintos — criar, alterar, remover e classificar lançamento — e a rotina diária de caixa
/// não passa por Command nenhum. Cada behavior chama daqui o que precisa; a lógica de decidir
/// o que emitir, encerrar ou calar mora num lugar só.
/// </remarks>
public interface IAlertaService
{
    /// <summary>
    /// Reavalia o estouro de orçamento de todas as categorias do mês.
    /// </summary>
    /// <remarks>
    /// Reavalia o mês inteiro, e não só a categoria tocada: reclassificar um lançamento move
    /// valor entre duas categorias de uma vez, e olhar só a de destino deixaria a de origem
    /// alertada depois de já ter voltado para dentro do plano.
    /// </remarks>
    Task<ResultadoDaAvaliacao> AvaliarCompetenciaAsync(
        DateOnly competencia,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reavalia a suficiência de caixa projetada na janela a partir de hoje.
    /// </summary>
    Task<ResultadoDaAvaliacao> AvaliarCaixaAsync(CancellationToken cancellationToken);
}
