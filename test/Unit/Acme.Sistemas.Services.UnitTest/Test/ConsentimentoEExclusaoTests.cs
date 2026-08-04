using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Services.V1.Identidade.Command.ExcluirConta;
using Acme.Sistemas.Services.V1.Identidade.Command.RegistrarConsentimento;
using Acme.Sistemas.Services.V1.Identidade.Query.ConsentimentoVigente;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

public sealed class ConsentimentoEExclusaoTests
{
    private static readonly Guid UsuarioId = Guid.CreateVersion7();

    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }

    private static readonly TimeProvider Relogio =
        new RelogioFixo(new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero));

    private static ConsentimentoEntity Aceite(string documento, string versao) => new()
    {
        Id = Guid.CreateVersion7(),
        UsuarioId = UsuarioId,
        Documento = documento,
        Versao = versao,
        AceitoEm = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Consentimento_e_registrado_como_linha_nova_preservando_o_historico()
    {
        var usuarios = new Mock<IUsuarioWriteRepository>();
        var handler = new RegistrarConsentimentoCommandHandler(usuarios.Object, Relogio);

        var resultado = await handler.HandleAsync(
            new RegistrarConsentimentoCommand(UsuarioId, DocumentoConsentimento.TermosDeUso, "2026-02"),
            CancellationToken.None);

        Assert.True(resultado.Sucesso);
        usuarios.Verify(
            r => r.RegistrarConsentimentoAsync(
                It.Is<ConsentimentoEntity>(c => c.Versao == "2026-02" && c.UsuarioId == UsuarioId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Documento_desconhecido_e_recusado()
    {
        var usuarios = new Mock<IUsuarioWriteRepository>();
        var handler = new RegistrarConsentimentoCommandHandler(usuarios.Object, Relogio);

        var resultado = await handler.HandleAsync(
            new RegistrarConsentimentoCommand(UsuarioId, "documento-inventado", "2026-02"),
            CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            RegistrarConsentimentoCommandValidation.CodigoDocumentoDesconhecido,
            resultado.Erros[0].Codigo);
        usuarios.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Versao_vigente_aceita_em_todos_os_documentos_libera()
    {
        var usuarios = new Mock<IUsuarioReadRepository>();
        usuarios.Setup(r => r.ObterConsentimentosAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DocumentoConsentimento.Obrigatorios.Select(d => Aceite(d, "2026-02")).ToList());

        var handler = new ConsentimentoVigenteQueryHandler(usuarios.Object);

        var resultado = await handler.HandleAsync(
            new ConsentimentoVigenteQuery(UsuarioId, "2026-02"), CancellationToken.None);

        Assert.True(resultado.Vigente);
        Assert.Empty(resultado.Pendentes);
    }

    [Fact]
    public async Task Documentos_atualizados_exigem_novo_aceite()
    {
        var usuarios = new Mock<IUsuarioReadRepository>();
        usuarios.Setup(r => r.ObterConsentimentosAsync(UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DocumentoConsentimento.Obrigatorios.Select(d => Aceite(d, "2026-01")).ToList());

        var handler = new ConsentimentoVigenteQueryHandler(usuarios.Object);

        var resultado = await handler.HandleAsync(
            new ConsentimentoVigenteQuery(UsuarioId, "2026-02"), CancellationToken.None);

        Assert.False(resultado.Vigente);
        Assert.Equal(DocumentoConsentimento.Obrigatorios.Length, resultado.Pendentes.Count);
    }

    [Fact]
    public async Task Exclusao_confirmada_delega_ao_repositorio()
    {
        var usuarios = new Mock<IUsuarioWriteRepository>();
        var handler = new ExcluirContaCommandHandler(usuarios.Object);

        var resultado = await handler.HandleAsync(
            new ExcluirContaCommand(UsuarioId, ConfirmacaoDoTitular: true), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        usuarios.Verify(r => r.ExcluirContaAsync(UsuarioId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Exclusao_sem_confirmacao_nao_apaga_nada()
    {
        var usuarios = new Mock<IUsuarioWriteRepository>();
        var handler = new ExcluirContaCommandHandler(usuarios.Object);

        var resultado = await handler.HandleAsync(
            new ExcluirContaCommand(UsuarioId, ConfirmacaoDoTitular: false), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(ExcluirContaCommandValidation.CodigoConfirmacaoAusente, resultado.Erros[0].Codigo);
        usuarios.VerifyNoOtherCalls();
    }
}
