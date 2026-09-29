using Acme.Sistemas.Core.Security;
using Acme.Sistemas.Domain.Entities.Administracao;
using Acme.Sistemas.Domain.Entities.Assinaturas;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Services.V1.Administracao.Command.AutenticarAdministrador;
using Acme.Sistemas.Services.V1.Administracao.Command.SalvarPlano;
using Acme.Sistemas.Services.V1.Administracao.Query.ListarContas;
using Acme.Sistemas.Domain.Enums;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

/// <summary>
/// A lógica administrativa isolada: o login que separa a operação do usuário final, a manutenção
/// do catálogo que distingue criar de atualizar e a relação de contas. O isolamento no banco e a
/// exclusividade das rotas são provados pela integração.
/// </summary>
public sealed class AdministracaoTests
{
    private static readonly Guid Administrador = Guid.CreateVersion7();
    private static readonly DateTimeOffset Agora = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Agora;
    }

    private static IGeradorDeToken Gerador()
    {
        var gerador = new Mock<IGeradorDeToken>();
        gerador.Setup(g => g.EmitirAdministradorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenEmitido("token", Agora.AddHours(1)));
        return gerador.Object;
    }

    // ---------------------------------------------------------------- autenticar

    /// <summary>Sem administrador com o e-mail, o login é recusado com a mensagem única.</summary>
    [Fact]
    public async Task Autenticar_sem_administrador_e_recusado()
    {
        var administradores = new Mock<IAdministradorRepository>();
        administradores.Setup(a => a.ObterCredenciaisAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CredenciaisAdministrativas?)null);

        var resultado = await new AutenticarAdministradorCommandHandler(administradores.Object, Gerador())
            .HandleAsync(new AutenticarAdministradorCommand("op@homefinance.local", "senha"), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(AutenticarAdministradorCommandHandler.CodigoCredencialInvalida, resultado.Erros[0].Codigo);
    }

    /// <summary>Senha errada é recusada com o mesmo código de e-mail inexistente.</summary>
    [Fact]
    public async Task Autenticar_com_senha_errada_e_recusado()
    {
        var administradores = new Mock<IAdministradorRepository>();
        administradores.Setup(a => a.ObterCredenciaisAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CredenciaisAdministrativas(Administrador, Hash.GerarHashSenha("senha-certa")));

        var resultado = await new AutenticarAdministradorCommandHandler(administradores.Object, Gerador())
            .HandleAsync(new AutenticarAdministradorCommand("op@homefinance.local", "senha-errada"), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(AutenticarAdministradorCommandHandler.CodigoCredencialInvalida, resultado.Erros[0].Codigo);
    }

    /// <summary>Credencial correta autentica e emite o token administrativo.</summary>
    [Fact]
    public async Task Autenticar_com_credencial_correta_emite_token()
    {
        var administradores = new Mock<IAdministradorRepository>();
        administradores.Setup(a => a.ObterCredenciaisAsync("op@homefinance.local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CredenciaisAdministrativas(Administrador, Hash.GerarHashSenha("senha-certa")));

        var resultado = await new AutenticarAdministradorCommandHandler(administradores.Object, Gerador())
            .HandleAsync(new AutenticarAdministradorCommand("op@homefinance.local", "senha-certa"), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("token", resultado.Token);
    }

    // ---------------------------------------------------------------- salvar plano

    /// <summary>Código novo cria o plano, com id e data de criação novos.</summary>
    [Fact]
    public async Task Salvar_plano_novo_cria()
    {
        var leitura = new Mock<IPlanoReadRepository>();
        leitura.Setup(p => p.ObterPorCodigoAsync("premium", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlanoEntity?)null);
        var escrita = new Mock<IPlanoWriteRepository>();

        var resultado = await new SalvarPlanoCommandHandler(leitura.Object, escrita.Object, new RelogioFixo())
            .HandleAsync(new SalvarPlanoCommand("premium", "Premium", 5, 49.90m, true), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.True(resultado.Criado);
        escrita.Verify(e => e.SalvarAsync(It.Is<PlanoEntity>(p => p.Codigo == "premium" && p.CriadoEm == Agora), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Código existente atualiza, conservando o id e a data de criação do plano.</summary>
    [Fact]
    public async Task Salvar_plano_existente_atualiza_conservando_id()
    {
        var idExistente = Guid.CreateVersion7();
        var criadoOriginal = Agora.AddYears(-1);

        var leitura = new Mock<IPlanoReadRepository>();
        leitura.Setup(p => p.ObterPorCodigoAsync("casal", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlanoEntity
            {
                Id = idExistente,
                Codigo = "casal",
                Nome = "Casal",
                CotaCompartilhamentos = 1,
                PrecoMensal = 29.90m,
                Ativo = true,
                CriadoEm = criadoOriginal,
            });
        var escrita = new Mock<IPlanoWriteRepository>();

        var resultado = await new SalvarPlanoCommandHandler(leitura.Object, escrita.Object, new RelogioFixo())
            .HandleAsync(new SalvarPlanoCommand("casal", "Casal", 2, 34.90m, true), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.False(resultado.Criado);
        Assert.Equal(idExistente, resultado.PlanoId);
        escrita.Verify(e => e.SalvarAsync(
            It.Is<PlanoEntity>(p => p.Id == idExistente && p.CriadoEm == criadoOriginal && p.CotaCompartilhamentos == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Cota negativa é recusada e nada é gravado.</summary>
    [Fact]
    public async Task Salvar_plano_com_cota_negativa_e_recusado()
    {
        var escrita = new Mock<IPlanoWriteRepository>();

        var resultado = await new SalvarPlanoCommandHandler(
                new Mock<IPlanoReadRepository>().Object, escrita.Object, new RelogioFixo())
            .HandleAsync(new SalvarPlanoCommand("x", "X", -1, 10m, true), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(SalvarPlanoCommandValidation.CotaInvalida, resultado.Erros[0].Codigo);
        escrita.Verify(e => e.SalvarAsync(It.IsAny<PlanoEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------------------------------------------------------------- listar contas

    /// <summary>A relação de contas repassa a leitura administrativa, sem dado financeiro.</summary>
    [Fact]
    public async Task Listar_contas_mapeia_o_que_a_leitura_devolve()
    {
        var usuarioId = Guid.CreateVersion7();
        var contas = new Mock<IAdministracaoReadRepository>();
        contas.Setup(c => c.ListarContasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContaAdministrativa>
            {
                new(usuarioId, "cliente@teste.local", Agora, SituacaoAssinatura.Ativa, "casal", "Casal"),
            });

        var resultado = await new ListarContasQueryHandler(contas.Object)
            .HandleAsync(new ListarContasQuery(), CancellationToken.None);

        Assert.Single(resultado.Contas);
        Assert.Equal(usuarioId, resultado.Contas[0].UsuarioId);
        Assert.Equal(SituacaoAssinatura.Ativa, resultado.Contas[0].SituacaoAssinatura);
    }
}
