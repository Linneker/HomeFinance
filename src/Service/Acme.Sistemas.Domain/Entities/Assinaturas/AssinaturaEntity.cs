using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Domain.Entities.Assinaturas;

/// <summary>
/// A assinatura de uma conta do aplicativo.
/// </summary>
/// <remarks>
/// Presa ao usuário, não ao tenant: quem paga é a pessoa, e ela leva a assinatura para qualquer
/// espaço que abrir. Presa também à conta do aplicativo, e não à identidade na loja — trocar de
/// conta na App Store não pode levar a assinatura embora, nem uma conta de loja pode ativar
/// duas contas do produto.
/// <para>
/// <see cref="ComprovanteReferencia"/> é o que o provedor devolveu. Nenhum dado de cartão passa
/// por aqui: o que se guarda é identificador, e identificador vazado não compra nada.
/// </para>
/// </remarks>
public sealed class AssinaturaEntity : BaseEntity
{
    public Guid UsuarioId { get; set; }

    public Guid PlanoId { get; set; }

    public SituacaoAssinatura Situacao { get; set; }

    public CanalPagamento Canal { get; set; }

    /// <summary>Até quando o período pago vale.</summary>
    public DateTimeOffset PeriodoFim { get; set; }

    /// <summary>
    /// Fim da tolerância depois do vencimento. Nulo enquanto a assinatura não entrou em carência.
    /// </summary>
    public DateTimeOffset? CarenciaAte { get; set; }

    /// <summary>Identificador da assinatura no provedor. Único por canal.</summary>
    public required string ComprovanteReferencia { get; set; }

    public DateTimeOffset? AtualizadoEm { get; set; }

    /// <summary>
    /// A conta opera inteira: ativa ou dentro da carência.
    /// </summary>
    /// <remarks>
    /// É esta pergunta que decide o acesso aos recursos derivados, e não <c>Situacao == Ativa</c>.
    /// Comparar com Ativa direto cortaria os recursos de quem está em carência, que é justamente
    /// quem o produto decidiu tolerar.
    /// </remarks>
    public bool Vigente => Situacao is SituacaoAssinatura.Ativa or SituacaoAssinatura.Carencia;

    /// <summary>
    /// Aplica a passagem do tempo: vencida vira carência, carência esgotada vira suspensa.
    /// </summary>
    /// <remarks>
    /// Calculada na leitura, e não por rotina. Uma assinatura só precisa estar correta no
    /// instante em que alguém pergunta por ela, e depender de rotina faria a conta operar
    /// suspensa — ou liberada — pelo tempo que a rotina demorasse a rodar.
    /// </remarks>
    public SituacaoAssinatura SituacaoEm(DateTimeOffset agora, TimeSpan carencia)
    {
        if (Situacao is SituacaoAssinatura.Cancelada or SituacaoAssinatura.Suspensa)
        {
            return Situacao;
        }

        if (agora <= PeriodoFim)
        {
            return SituacaoAssinatura.Ativa;
        }

        var limite = CarenciaAte ?? PeriodoFim.Add(carencia);

        return agora <= limite ? SituacaoAssinatura.Carencia : SituacaoAssinatura.Suspensa;
    }
}
