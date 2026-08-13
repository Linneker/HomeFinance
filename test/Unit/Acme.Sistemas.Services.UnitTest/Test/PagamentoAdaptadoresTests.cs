using System.Net;
using System.Net.Http.Json;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.ExternalIntegration.Pagamentos;
using Acme.Sistemas.Infrastructure.Secrets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Acme.Sistemas.Services.UnitTest.Test;

/// <summary>
/// Os adaptadores de pagamento: a tradução da resposta de cada provedor em "está pago e até
/// quando?", e a garantia de que só identificador trafega — nunca dado de cartão.
/// </summary>
/// <remarks>
/// É a fronteira HTTP, e por isso a parte que os testes de serviço não alcançam: eles usam um
/// <see cref="IProvedorDePagamento"/> dublê. Aqui o dublê é o servidor do provedor, e o que está
/// sob prova é o adaptador de verdade — inclusive que a credencial vem do cofre a cada chamada e
/// que o corpo enviado não carrega nada além do comprovante.
/// </remarks>
public sealed class PagamentoAdaptadoresTests
{
    private static readonly DateTimeOffset Vencimento = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Faz o papel do servidor do provedor e guarda o que o adaptador enviou.</summary>
    private sealed class ProvedorNaRede(Func<HttpResponseMessage> responder) : HttpMessageHandler
    {
        public HttpRequestMessage? Requisicao { get; private set; }

        public string? CorpoEnviado { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requisicao = request;

            if (request.Content is not null)
            {
                CorpoEnviado = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return responder();
        }
    }

    private sealed class SegredoDoCofre(string valor) : ISecretProvider
    {
        public List<string> Chaves { get; } = [];

        public Task<string> GetAsync(string chave, CancellationToken cancellationToken = default)
        {
            Chaves.Add(chave);
            return Task.FromResult(valor);
        }
    }

    private static HttpResponseMessage Resposta(HttpStatusCode status, object? corpo = null) =>
        new(status) { Content = corpo is null ? null : JsonContent.Create(corpo) };

    private static IOptions<OpcoesDePagamento> Opcoes() => Options.Create(new OpcoesDePagamento());

    // ---------------------------------------------------------------- compra in-app

    private static CompraInAppProvedor Loja(
        CanalPagamento canal, ProvedorNaRede rede, ISecretProvider segredos) =>
        new(new HttpClient(rede), segredos, Opcoes(),
            NullLogger<CompraInAppProvedor>.Instance, canal);

    /// <summary>Recibo válido vira assinatura ativa, com a referência e o vencimento da loja.</summary>
    [Fact]
    public async Task Loja_confirma_recibo_valido()
    {
        var rede = new ProvedorNaRede(() => Resposta(
            HttpStatusCode.OK,
            new { valido = true, referencia = "GPA.777", expiraEm = Vencimento, motivo = (string?)null }));

        var confirmacao = await Loja(CanalPagamento.GooglePlay, rede, new SegredoDoCofre("cred"))
            .ConfirmarAsync("recibo", CancellationToken.None);

        Assert.True(confirmacao.Confirmado);
        Assert.Equal("GPA.777", confirmacao.Referencia);
        Assert.Equal(Vencimento, confirmacao.PeriodoFim);
    }

    /// <summary>Recibo que a loja não valida não concede direito, e devolve o motivo dela.</summary>
    [Fact]
    public async Task Loja_recusa_recibo_invalido_com_o_motivo()
    {
        var rede = new ProvedorNaRede(() => Resposta(
            HttpStatusCode.OK,
            new { valido = false, referencia = "", expiraEm = (DateTimeOffset?)null, motivo = "recibo_expirado" }));

        var confirmacao = await Loja(CanalPagamento.AppStore, rede, new SegredoDoCofre("cred"))
            .ConfirmarAsync("recibo", CancellationToken.None);

        Assert.False(confirmacao.Confirmado);
        Assert.Equal("recibo_expirado", confirmacao.Motivo);
    }

    /// <summary>
    /// Loja fora do ar não é recusa de compra: cai como indisponível, para não conceder direito
    /// nem tirar o que já havia por causa de uma falha de rede.
    /// </summary>
    [Fact]
    public async Task Loja_indisponivel_nao_confirma_nem_nega()
    {
        var rede = new ProvedorNaRede(() => Resposta(HttpStatusCode.ServiceUnavailable));

        var confirmacao = await Loja(CanalPagamento.GooglePlay, rede, new SegredoDoCofre("cred"))
            .ConfirmarAsync("recibo", CancellationToken.None);

        Assert.False(confirmacao.Confirmado);
        Assert.Equal("provedor_indisponivel", confirmacao.Motivo);
    }

    /// <summary>Comprovante vazio nem chega a bater na loja.</summary>
    [Fact]
    public async Task Loja_com_comprovante_vazio_recusa_sem_chamar_a_rede()
    {
        var rede = new ProvedorNaRede(() => Resposta(HttpStatusCode.OK));

        var confirmacao = await Loja(CanalPagamento.GooglePlay, rede, new SegredoDoCofre("cred"))
            .ConfirmarAsync("   ", CancellationToken.None);

        Assert.False(confirmacao.Confirmado);
        Assert.Equal("comprovante_ausente", confirmacao.Motivo);
        Assert.Null(rede.Requisicao);
    }

    /// <summary>
    /// A credencial vem do cofre, pela chave do canal — nunca de <c>appsettings</c> —, e o corpo
    /// enviado carrega só o comprovante e a credencial: nenhum dado de cartão trafega.
    /// </summary>
    [Fact]
    public async Task Loja_busca_a_credencial_no_cofre_e_nao_envia_dado_de_cartao()
    {
        var rede = new ProvedorNaRede(() => Resposta(
            HttpStatusCode.OK,
            new { valido = true, referencia = "APP.1", expiraEm = Vencimento, motivo = (string?)null }));
        var segredos = new SegredoDoCofre("credencial-secreta");

        await Loja(CanalPagamento.AppStore, rede, segredos)
            .ConfirmarAsync("meu-recibo", CancellationToken.None);

        Assert.Equal(ChavesDeSegredo.AppStore, Assert.Single(segredos.Chaves));
        Assert.Contains("meu-recibo", rede.CorpoEnviado);
        Assert.Contains("credencial-secreta", rede.CorpoEnviado);
        // O corpo tem exatamente dois campos — não há onde um número de cartão entrar.
        Assert.DoesNotContain("cart", rede.CorpoEnviado, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cvv", rede.CorpoEnviado, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------- gateways secundários

    private static GatewayDePagamento Gateway(
        CanalPagamento canal, ProvedorNaRede rede, ISecretProvider segredos) =>
        new(new HttpClient(rede), segredos, Opcoes(),
            NullLogger<GatewayDePagamento>.Instance, canal);

    /// <summary>Assinatura ativa no gateway vira contratação confirmada.</summary>
    [Theory]
    [InlineData(CanalPagamento.MercadoPago, "authorized")]
    [InlineData(CanalPagamento.Asaas, "ACTIVE")]
    public async Task Gateway_confirma_assinatura_ativa(CanalPagamento canal, string status)
    {
        var rede = new ProvedorNaRede(() => Resposta(
            HttpStatusCode.OK, new { status, proximoVencimento = Vencimento }));

        var confirmacao = await Gateway(canal, rede, new SegredoDoCofre("cred"))
            .ConsultarAsync("SUB-1", CancellationToken.None);

        Assert.True(confirmacao.Confirmado);
        Assert.Equal("SUB-1", confirmacao.Referencia);
        Assert.Equal(Vencimento, confirmacao.PeriodoFim);
    }

    /// <summary>Assinatura que não está ativa no gateway não confirma.</summary>
    [Fact]
    public async Task Gateway_recusa_assinatura_nao_ativa()
    {
        var rede = new ProvedorNaRede(() => Resposta(
            HttpStatusCode.OK, new { status = "cancelled", proximoVencimento = (DateTimeOffset?)null }));

        var confirmacao = await Gateway(CanalPagamento.MercadoPago, rede, new SegredoDoCofre("cred"))
            .ConsultarAsync("SUB-2", CancellationToken.None);

        Assert.False(confirmacao.Confirmado);
    }

    /// <summary>Gateway fora do ar cai como indisponível, não como recusa.</summary>
    [Fact]
    public async Task Gateway_indisponivel_nao_altera()
    {
        var rede = new ProvedorNaRede(() => Resposta(HttpStatusCode.InternalServerError));

        var confirmacao = await Gateway(CanalPagamento.Asaas, rede, new SegredoDoCofre("cred"))
            .ConsultarAsync("SUB-3", CancellationToken.None);

        Assert.False(confirmacao.Confirmado);
        Assert.Equal("provedor_indisponivel", confirmacao.Motivo);
    }

    /// <summary>Referência vazia não chega a bater no gateway.</summary>
    [Fact]
    public async Task Gateway_com_referencia_vazia_recusa_sem_chamar_a_rede()
    {
        var rede = new ProvedorNaRede(() => Resposta(HttpStatusCode.OK));

        var confirmacao = await Gateway(CanalPagamento.MercadoPago, rede, new SegredoDoCofre("cred"))
            .ConsultarAsync("", CancellationToken.None);

        Assert.False(confirmacao.Confirmado);
        Assert.Equal("referencia_ausente", confirmacao.Motivo);
        Assert.Null(rede.Requisicao);
    }

    /// <summary>
    /// Mercado Pago autentica por Bearer; Asaas por cabeçalho próprio. A credencial vem do cofre,
    /// pela chave do canal, e só o identificador da assinatura vai na URL — nenhum dado de cartão.
    /// </summary>
    [Theory]
    [InlineData(CanalPagamento.MercadoPago, ChavesDeSegredo.MercadoPago)]
    [InlineData(CanalPagamento.Asaas, ChavesDeSegredo.Asaas)]
    public async Task Gateway_autentica_com_credencial_do_cofre_e_so_o_identificador_na_url(
        CanalPagamento canal, string chaveEsperada)
    {
        var rede = new ProvedorNaRede(() => Resposta(
            HttpStatusCode.OK, new { status = "active", proximoVencimento = Vencimento }));
        var segredos = new SegredoDoCofre("token-do-cofre");

        await Gateway(canal, rede, segredos).ConsultarAsync("SUB-42", CancellationToken.None);

        Assert.Equal(chaveEsperada, Assert.Single(segredos.Chaves));
        Assert.Contains("SUB-42", rede.Requisicao!.RequestUri!.ToString());

        if (canal == CanalPagamento.MercadoPago)
        {
            Assert.Equal("Bearer", rede.Requisicao.Headers.Authorization!.Scheme);
            Assert.Equal("token-do-cofre", rede.Requisicao.Headers.Authorization.Parameter);
        }
        else
        {
            Assert.Equal("token-do-cofre", Assert.Single(rede.Requisicao.Headers.GetValues("access_token")));
        }
    }
}
