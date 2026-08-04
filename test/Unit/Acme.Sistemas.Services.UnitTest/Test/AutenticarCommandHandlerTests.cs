using Acme.Sistemas.Core.Security;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Services.V1.Identidade.Command.Autenticar;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

public sealed class AutenticarCommandHandlerTests
{
    private const string SenhaCorreta = "Senha#Forte1";

    private static readonly Guid UsuarioId = Guid.CreateVersion7();
    private static readonly Guid TenantId = Guid.CreateVersion7();

    private static (AutenticarCommandHandler Handler, Mock<IGeradorDeToken> Gerador) Montar(
        CredenciaisDeAcesso? credenciais)
    {
        var usuarios = new Mock<IUsuarioWriteRepository>();
        usuarios.Setup(r => r.ObterCredenciaisAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credenciais);

        var gerador = new Mock<IGeradorDeToken>();
        gerador.Setup(g => g.EmitirAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenEmitido("token-emitido", DateTimeOffset.UtcNow.AddMinutes(30)));

        return (new AutenticarCommandHandler(usuarios.Object, gerador.Object), gerador);
    }

    private static CredenciaisDeAcesso CredenciaisValidas() =>
        new(UsuarioId, Hash.GerarHashSenha(SenhaCorreta), TenantId);

    [Fact]
    public async Task Deve_emitir_token_com_usuario_e_tenant_padrao()
    {
        var (handler, gerador) = Montar(CredenciaisValidas());

        var resultado = await handler.HandleAsync(
            new AutenticarCommand("ana@exemplo.com.br", SenhaCorreta), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("token-emitido", resultado.Token);
        gerador.Verify(g => g.EmitirAsync(UsuarioId, TenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Senha_incorreta_nao_emite_token()
    {
        var (handler, gerador) = Montar(CredenciaisValidas());

        var resultado = await handler.HandleAsync(
            new AutenticarCommand("ana@exemplo.com.br", "Outra#Senha1"), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Null(resultado.Token);
        gerador.Verify(g => g.EmitirAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Email_inexistente_e_senha_errada_devolvem_a_mesma_mensagem()
    {
        var (handlerSemConta, _) = Montar(credenciais: null);
        var (handlerSenhaErrada, _) = Montar(CredenciaisValidas());

        var semConta = await handlerSemConta.HandleAsync(
            new AutenticarCommand("nao-existe@exemplo.com.br", SenhaCorreta), CancellationToken.None);

        var senhaErrada = await handlerSenhaErrada.HandleAsync(
            new AutenticarCommand("ana@exemplo.com.br", "Errada#123"), CancellationToken.None);

        // Distinguir os dois casos entregaria ao atacante a lista de e-mails cadastrados.
        Assert.False(semConta.Sucesso);
        Assert.False(senhaErrada.Sucesso);
        Assert.Equal(senhaErrada.Erros[0].Codigo, semConta.Erros[0].Codigo);
        Assert.Equal(senhaErrada.Erros[0].Mensagem, semConta.Erros[0].Mensagem);
    }

    [Theory]
    [InlineData("", SenhaCorreta)]
    [InlineData("ana@exemplo.com.br", "")]
    public async Task Campos_obrigatorios_ausentes_sao_recusados_sem_consultar_o_banco(
        string email, string senha)
    {
        var usuarios = new Mock<IUsuarioWriteRepository>();
        var gerador = new Mock<IGeradorDeToken>();
        var handler = new AutenticarCommandHandler(usuarios.Object, gerador.Object);

        var resultado = await handler.HandleAsync(new AutenticarCommand(email, senha), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        usuarios.VerifyNoOtherCalls();
    }
}
