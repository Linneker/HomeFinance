using Acme.Sistemas.Core.Security;
using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Services.V1.Identidade.Command.CriarConta;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

public sealed class CriarContaCommandHandlerTests
{
    private static readonly TimeProvider Relogio =
        new FakeTimeProvider(new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero));

    private sealed class FakeTimeProvider(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }

    private static CriarContaCommand Comando(
        string email = "ana@exemplo.com.br",
        string senha = "Senha#Forte1",
        bool aceitou = true) => new(email, senha, aceitou, "2026-01");

    [Fact]
    public async Task Deve_criar_conta_tenant_e_consentimentos_num_unico_passo()
    {
        var usuarios = new Mock<IUsuarioWriteRepository>();
        usuarios.Setup(r => r.ExisteEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        UsuarioEntity? gravado = null;
        TenantEntity? tenantGravado = null;
        IReadOnlyList<ConsentimentoEntity> consentimentos = [];

        usuarios.Setup(r => r.CriarContaAsync(
                It.IsAny<UsuarioEntity>(),
                It.IsAny<TenantEntity>(),
                It.IsAny<IReadOnlyList<ConsentimentoEntity>>(),
                It.IsAny<CancellationToken>()))
            .Callback<UsuarioEntity, TenantEntity, IReadOnlyList<ConsentimentoEntity>, CancellationToken>(
                (u, t, c, _) => (gravado, tenantGravado, consentimentos) = (u, t, c))
            .Returns(Task.CompletedTask);

        var handler = new CriarContaCommandHandler(usuarios.Object, Relogio);

        var resultado = await handler.HandleAsync(Comando(), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.NotNull(gravado);
        Assert.NotNull(tenantGravado);
        Assert.Equal(gravado!.Id, tenantGravado!.UsuarioId);

        // Ambos os documentos obrigatórios registrados com versão — é o que prova o aceite.
        Assert.Equal(DocumentoConsentimento.Obrigatorios.Length, consentimentos.Count);
        Assert.All(consentimentos, c => Assert.Equal("2026-01", c.Versao));
    }

    [Fact]
    public async Task Nao_deve_armazenar_a_senha_em_texto_claro()
    {
        var usuarios = new Mock<IUsuarioWriteRepository>();
        usuarios.Setup(r => r.ExisteEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        UsuarioEntity? gravado = null;
        usuarios.Setup(r => r.CriarContaAsync(
                It.IsAny<UsuarioEntity>(), It.IsAny<TenantEntity>(),
                It.IsAny<IReadOnlyList<ConsentimentoEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<UsuarioEntity, TenantEntity, IReadOnlyList<ConsentimentoEntity>, CancellationToken>(
                (u, _, _, _) => gravado = u)
            .Returns(Task.CompletedTask);

        var handler = new CriarContaCommandHandler(usuarios.Object, Relogio);

        await handler.HandleAsync(Comando(senha: "Senha#Forte1"), CancellationToken.None);

        Assert.DoesNotContain("Senha#Forte1", gravado!.SenhaHash, StringComparison.Ordinal);
        Assert.True(Hash.SenhaConfere("Senha#Forte1", gravado.SenhaHash));
        Assert.False(Hash.SenhaConfere("Senha#Forte2", gravado.SenhaHash));
    }

    [Fact]
    public async Task Deve_recusar_email_ja_cadastrado_sem_revelar_dado_da_conta()
    {
        var usuarios = new Mock<IUsuarioWriteRepository>();
        usuarios.Setup(r => r.ExisteEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CriarContaCommandHandler(usuarios.Object, Relogio);

        var resultado = await handler.HandleAsync(Comando(), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Contains(resultado.Erros, e => e.Codigo == CriarContaCommandHandler.CodigoEmailEmUso);
        usuarios.Verify(
            r => r.CriarContaAsync(
                It.IsAny<UsuarioEntity>(), It.IsAny<TenantEntity>(),
                It.IsAny<IReadOnlyList<ConsentimentoEntity>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Deve_recusar_senha_fora_da_politica_descrevendo_os_criterios()
    {
        var usuarios = new Mock<IUsuarioWriteRepository>();
        var handler = new CriarContaCommandHandler(usuarios.Object, Relogio);

        var resultado = await handler.HandleAsync(Comando(senha: "abc"), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.All(
            resultado.Erros.Where(e => e.Codigo == CriarContaCommandValidation.CodigoSenhaFraca),
            e => Assert.False(string.IsNullOrWhiteSpace(e.Mensagem)));

        // Recusa por validação não chega a consultar o banco.
        usuarios.Verify(
            r => r.ExisteEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Sem_aceite_dos_termos_nenhum_dado_pessoal_e_persistido()
    {
        var usuarios = new Mock<IUsuarioWriteRepository>();
        var handler = new CriarContaCommandHandler(usuarios.Object, Relogio);

        var resultado = await handler.HandleAsync(Comando(aceitou: false), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Contains(resultado.Erros, e => e.Codigo == CriarContaCommandValidation.CodigoTermosNaoAceitos);
        usuarios.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("sem-arroba")]
    [InlineData("ana@semponto")]
    [InlineData("")]
    public async Task Deve_recusar_email_invalido(string email)
    {
        var usuarios = new Mock<IUsuarioWriteRepository>();
        var handler = new CriarContaCommandHandler(usuarios.Object, Relogio);

        var resultado = await handler.HandleAsync(Comando(email: email), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Contains(resultado.Erros, e => e.Codigo == CriarContaCommandValidation.CodigoEmailInvalido);
    }
}
