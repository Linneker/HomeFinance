using Acme.Sistemas.HomeFinance.Api.Endpoints.V1.Assinatura;
using Acme.Sistemas.Services.V1.Assinaturas;

namespace Acme.Sistemas.IntegrationTest.Test;

/// <summary>
/// A tradução do rótulo que cada provedor manda para o evento do domínio.
/// </summary>
/// <remarks>
/// Não toca no banco — vive aqui porque o mapa é interno ao endpoint, visível ao projeto de
/// integração. O que se prova é a decisão de segurança: rótulo desconhecido cai em estorno, e não
/// em renovação. Entre suspender indevidamente — recuperável por uma reconciliação — e liberar de
/// graça um evento que não sabemos ler, o primeiro erro é o único que aparece em algum lugar.
/// </remarks>
public sealed class MapaDeNotificacaoTests
{
    [Theory]
    [InlineData("renewal")]
    [InlineData("renovacao")]
    [InlineData("payment.approved")]
    [InlineData("PAYMENT_CONFIRMED")]
    public void Rotulos_de_renovacao_viram_renovacao(string rotulo) =>
        Assert.Equal(EventoDoProvedor.Renovacao, rotulo.ParaEvento());

    [Theory]
    [InlineData("cancel")]
    [InlineData("cancelamento")]
    [InlineData("subscription.cancelled")]
    public void Rotulos_de_cancelamento_viram_cancelamento_agendado(string rotulo) =>
        Assert.Equal(EventoDoProvedor.CancelamentoAgendado, rotulo.ParaEvento());

    [Theory]
    [InlineData("refund")]
    [InlineData("chargeback")]
    [InlineData("")]
    [InlineData("um_evento_que_nao_conhecemos")]
    public void Rotulo_desconhecido_ou_de_estorno_vira_estorno(string rotulo) =>
        Assert.Equal(EventoDoProvedor.Estorno, rotulo.ParaEvento());
}
