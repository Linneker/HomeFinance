using System.Text;
using Acme.Sistemas.Core.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Acme.Sistemas.Services.UnitTest.Test;

/// <summary>
/// Expiração do token de sessão. O que se prova aqui é que um token vencido é recusado — e
/// recusado sem folga, porque o default do .NET tolera 5 minutos além do vencimento.
/// </summary>
public sealed class SessaoExpiradaTests
{
    private const string Chave = "chave-de-teste-com-no-minimo-64-bytes-para-hs512-0123456789abcdef";

    // O instante de validação é sempre "agora"; o que se desloca é o momento da EMISSÃO.
    // A biblioteca de validação não expõe injeção de relógio nesta versão, então controlar o
    // tempo pelo gerador é o que permite testar expiração sem esperar meia hora.
    private static DateTimeOffset Agora => DateTimeOffset.UtcNow;

    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }

    private sealed class ChaveFixa : IProvedorDeChaveJwt
    {
        public Task<string> ObterChaveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Chave);
    }

    private static readonly JwtOptions Opcoes = new()
    {
        Emissor = "acme.sistemas.homefinance",
        Audiencia = "acme.sistemas.homefinance.app",
        ValidadeAcesso = TimeSpan.FromMinutes(30),
    };

    private static GeradorDeToken Gerador(DateTimeOffset agora) =>
        new(Options.Create(Opcoes), new ChaveFixa(), new RelogioFixo(agora));

    private static TokenValidationParameters Parametros(TimeSpan folga) => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Opcoes.Emissor,
        ValidAudience = Opcoes.Audiencia,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Chave)),
        ClockSkew = folga,
    };

    private static Task<TokenValidationResult> ValidarAsync(string token, TimeSpan folga)
        => new JsonWebTokenHandler().ValidateTokenAsync(token, Parametros(folga));

    [Fact]
    public async Task Token_dentro_da_validade_e_aceito()
    {
        var emitido = await Gerador(Agora).EmitirAsync(Guid.CreateVersion7(), Guid.CreateVersion7());

        var resultado = await ValidarAsync(emitido.Token, TimeSpan.Zero);

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public async Task Token_expirado_e_recusado()
    {
        // Emitido duas horas atrás, com validade de trinta minutos.
        var emitido = await Gerador(Agora.AddHours(-2))
            .EmitirAsync(Guid.CreateVersion7(), Guid.CreateVersion7());

        var resultado = await ValidarAsync(emitido.Token, TimeSpan.Zero);

        Assert.False(resultado.IsValid);
        Assert.IsType<SecurityTokenExpiredException>(resultado.Exception);
    }

    [Fact]
    public async Task Sem_folga_o_token_vence_no_instante_declarado()
    {
        // Vencido há um minuto: dentro da folga padrão de cinco minutos, fora do zero.
        var emitido = await Gerador(Agora.AddMinutes(-31))
            .EmitirAsync(Guid.CreateVersion7(), Guid.CreateVersion7());

        var semFolga = await ValidarAsync(emitido.Token, TimeSpan.Zero);
        var comFolgaPadrao = await ValidarAsync(emitido.Token, TimeSpan.FromMinutes(5));

        // Com o ClockSkew padrão do .NET este token ainda valeria — é exatamente a folga que
        // a configuração da Api zera.
        Assert.False(semFolga.IsValid);
        Assert.True(comFolgaPadrao.IsValid);
    }

    [Fact]
    public async Task Token_assinado_com_outra_chave_e_recusado()
    {
        var emitido = await Gerador(Agora).EmitirAsync(Guid.CreateVersion7(), Guid.CreateVersion7());

        var parametros = Parametros(TimeSpan.Zero);
        parametros.IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(Chave.Replace('0', 'X')));

        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(emitido.Token, parametros);

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public async Task Chave_curta_nao_emite_token_fraco()
    {
        var geradorComChaveCurta = new GeradorDeToken(
            Options.Create(Opcoes), new ChaveCurta(), new RelogioFixo(Agora));

        // Falhar é melhor que assinar com chave insuficiente e ninguém perceber.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => geradorComChaveCurta.EmitirAsync(Guid.CreateVersion7(), Guid.CreateVersion7()));
    }

    private sealed class ChaveCurta : IProvedorDeChaveJwt
    {
        public Task<string> ObterChaveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult("curta-demais");
    }
}
