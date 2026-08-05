using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Entities.Planejamento;
using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.IntegrationTest.Config;
using Acme.Sistemas.Repository.Repositories.V1.Categoria;
using Acme.Sistemas.Repository.Repositories.V1.FonteRenda;
using Acme.Sistemas.Repository.Repositories.V1.Orcamento;
using Acme.Sistemas.Repository.Repositories.V1.Tenant;
using Acme.Sistemas.Repository.Repositories.V1.Usuario;
using Acme.Sistemas.Services.V1.Assinaturas;
using Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;
using Acme.Sistemas.Services.V1.Planejamento.Command.DefinirOrcamento;
using Acme.Sistemas.Services.V1.Planejamento.Query.FontesRenda;
using Acme.Sistemas.Services.V1.Planejamento.Query.OrcamentoDoMes;
using Moq;

namespace Acme.Sistemas.IntegrationTest.Test;

/// <summary>
/// O orçamento e a renda pertencem ao tenant: quem recebeu o compartilhamento enxerga os dois e
/// não altera nenhum dos dois.
/// </summary>
/// <remarks>
/// Precisa ser teste de integração. Nenhum handler de planejamento verifica modo somente leitura
/// — quem recusa a escrita é a policy de <c>fonte_renda</c> e <c>orcamento_mensal</c>. Com
/// repositório dublê os mesmos comandos passariam, e o teste provaria só que o dublê aceitou.
/// <para>
/// O modo de acesso não é escolhido aqui: sai da resolução real do compartilhamento, o mesmo
/// caminho que o middleware percorre.
/// </para>
/// </remarks>
[Collection(nameof(BancoCollection))]
public sealed class PlanejamentoCompartilhadoTests(AmbienteBanco ambiente)
{
    private static CancellationToken Ct => CancellationToken.None;

    private static readonly DateOnly Agosto = new(2026, 8, 1);

    private sealed record Conta(Guid UsuarioId, Guid TenantId, string Email);

    /// <summary>
    /// Assinatura ativa com cota folgada: o que está sob teste é o alcance da leitura
    /// compartilhada, e a cota tem cobertura própria nos testes de assinatura.
    /// </summary>
    private static IAssinaturaService ComCota()
    {
        var assinaturas = new Mock<IAssinaturaService>();

        assinaturas.Setup(a => a.SituacaoAtualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SituacaoDaConta(SituacaoAssinatura.Ativa, "casal", 3, 0));

        return assinaturas.Object;
    }

    private async Task<Conta> CriarContaAsync(ContextoFixo contexto)
    {
        var usuarioId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var email = $"{usuarioId:N}@teste.local";
        var agora = DateTimeOffset.UtcNow;

        contexto.UsuarioId = null;
        contexto.TenantId = null;
        contexto.SomenteLeitura = false;

        await new UsuarioWriteRepository(ambiente.CriarGerenciador(contexto)).CriarContaAsync(
            new UsuarioEntity { Id = usuarioId, Email = email, SenhaHash = "hash", CriadoEm = agora },
            new TenantEntity { Id = tenantId, UsuarioId = usuarioId, CriadoEm = agora },
            [.. DocumentoConsentimento.Obrigatorios.Select(documento => new ConsentimentoEntity
            {
                Id = Guid.CreateVersion7(),
                UsuarioId = usuarioId,
                Documento = documento,
                Versao = "1.0",
                AceitoEm = agora,
            })],
            Ct);

        return new Conta(usuarioId, tenantId, email);
    }

    private static void Ativar(ContextoFixo contexto, Conta conta, bool somenteLeitura)
    {
        contexto.UsuarioId = conta.UsuarioId;
        contexto.TenantId = conta.TenantId;
        contexto.SomenteLeitura = somenteLeitura;
    }

    private async Task<Guid> CriarCategoriaAsync(ContextoFixo contexto, Conta dono, string nome)
    {
        Ativar(contexto, dono, somenteLeitura: false);

        var categoria = new CategoriaEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = dono.TenantId,
            Nome = nome,
            CriadoEm = DateTimeOffset.UtcNow,
        };

        await new CategoriaWriteRepository(ambiente.CriarGerenciador(contexto))
            .CriarAsync(categoria, Ct);

        return categoria.Id;
    }

    private async Task<Guid> DeclararRendaAsync(
        ContextoFixo contexto, Conta dono, string nome, decimal valor)
    {
        Ativar(contexto, dono, somenteLeitura: false);

        var fonte = new FonteRendaEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = dono.TenantId,
            Nome = nome,
            Valor = valor,
            DiaRecebimento = 5,
            VigenciaInicio = Agosto,
            CriadoEm = DateTimeOffset.UtcNow,
        };

        await new FonteRendaWriteRepository(ambiente.CriarGerenciador(contexto))
            .GravarAsync(fonte, Ct);

        return fonte.Id;
    }

    private async Task OrcarAsync(
        ContextoFixo contexto, Conta dono, Guid categoriaId, decimal valor)
    {
        Ativar(contexto, dono, somenteLeitura: false);

        await new OrcamentoWriteRepository(ambiente.CriarGerenciador(contexto)).GravarAsync(
            new OrcamentoMensalEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = dono.TenantId,
                CategoriaId = categoriaId,
                Competencia = Agosto,
                ValorPlanejado = valor,
                CriadoEm = DateTimeOffset.UtcNow,
            },
            Ct);
    }

    /// <summary>
    /// Concede o acesso pelo Command real e devolve o acesso resolvido, como o middleware faria.
    /// </summary>
    private async Task<TenantDisponivel> CompartilharAsync(
        ContextoFixo contexto, Conta dono, Conta destinatario)
    {
        Ativar(contexto, dono, somenteLeitura: false);

        var concessao = await new CompartilharTenantCommandHandler(
            new TenantWriteRepository(ambiente.CriarGerenciador(contexto)),
            ComCota(),
            TimeProvider.System)
            .HandleAsync(
                new CompartilharTenantCommand(
                    dono.UsuarioId, dono.TenantId, destinatario.Email,
                    ConsentimentoConfirmado: true, IncluiScr: false),
                Ct);

        Assert.True(concessao.Sucesso);

        contexto.UsuarioId = destinatario.UsuarioId;
        contexto.TenantId = null;
        contexto.SomenteLeitura = true;

        var acesso = await new TenantWriteRepository(ambiente.CriarGerenciador(contexto))
            .ResolverAcessoAsync(destinatario.UsuarioId, dono.TenantId, Ct);

        Assert.NotNull(acesso);
        Assert.False(acesso.EhProprio);
        Assert.True(acesso.SomenteLeitura);

        contexto.UsuarioId = destinatario.UsuarioId;
        contexto.TenantId = acesso.TenantId;
        contexto.SomenteLeitura = acesso.SomenteLeitura;

        return acesso;
    }

    /// <summary>
    /// Cenário literal da spec: Bruno abre o tenant compartilhado da Ana e vê o orçamento dela.
    /// </summary>
    [Fact]
    public async Task Destinatario_ve_o_orcamento_e_a_renda_do_dono()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);
        var bruno = await CriarContaAsync(contexto);

        var alimentacao = await CriarCategoriaAsync(contexto, ana, "Alimentação");

        await DeclararRendaAsync(contexto, ana, "Salário", 6000m);
        await OrcarAsync(contexto, ana, alimentacao, 1200m);

        // Bruno também planeja a vida dele. Se o recorte falhar, os números se somam.
        var transporteDoBruno = await CriarCategoriaAsync(contexto, bruno, "Transporte");
        await DeclararRendaAsync(contexto, bruno, "Salário do Bruno", 999m);
        await OrcarAsync(contexto, bruno, transporteDoBruno, 777m);

        await CompartilharAsync(contexto, ana, bruno);

        var gerenciador = ambiente.CriarGerenciador(contexto);

        var orcamento = await new OrcamentoDoMesQueryHandler(
                new OrcamentoReadRepository(gerenciador),
                new FonteRendaReadRepository(gerenciador))
            .HandleAsync(new OrcamentoDoMesQuery(ana.TenantId, Agosto), Ct);

        var categoria = Assert.Single(orcamento.Categorias);

        Assert.Equal(alimentacao, categoria.CategoriaId);
        Assert.Equal(1200m, categoria.ValorPlanejado);
        Assert.Equal(1200m, orcamento.TotalPlanejado);
        Assert.Equal(6000m, orcamento.RendaPrevista);

        var rendas = await new FontesRendaQueryHandler(new FonteRendaReadRepository(gerenciador))
            .HandleAsync(new FontesRendaQuery(ana.TenantId, Agosto), Ct);

        var fonte = Assert.Single(rendas.Fontes);

        Assert.Equal("Salário", fonte.Nome);
        Assert.Equal(6000m, rendas.RendaPrevista);
    }

    /// <summary>
    /// "e não consegue alterá-lo": quem recusa é a policy de INSERT, não o handler. Vai pelo
    /// Command real justamente para provar que o caminho completo esbarra no banco.
    /// </summary>
    [Fact]
    public async Task Destinatario_nao_consegue_definir_orcamento_no_tenant_do_dono()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);
        var bruno = await CriarContaAsync(contexto);

        var lazer = await CriarCategoriaAsync(contexto, ana, "Lazer");

        await DeclararRendaAsync(contexto, ana, "Salário", 6000m);
        await OrcarAsync(contexto, ana, lazer, 300m);

        await CompartilharAsync(contexto, ana, bruno);

        var gerenciador = ambiente.CriarGerenciador(contexto);

        var handler = new DefinirOrcamentoCommandHandler(
            new OrcamentoWriteRepository(gerenciador),
            new CategoriaWriteRepository(gerenciador),
            new FonteRendaWriteRepository(gerenciador),
            TimeProvider.System);

        await Assert.ThrowsAnyAsync<DbException>(async () =>
            await handler.HandleAsync(
                new DefinirOrcamentoCommand(ana.TenantId, lazer, Agosto, 5000m), Ct));

        // O valor da Ana continua o que ela definiu.
        Ativar(contexto, ana, somenteLeitura: false);

        var doMes = await new OrcamentoReadRepository(ambiente.CriarGerenciador(contexto))
            .ListarDoMesAsync(Agosto, Ct);

        var linha = Assert.Single(doMes);

        Assert.Equal(300m, linha.ValorPlanejado);
    }

    /// <summary>
    /// UPDATE e DELETE não estouram: a policy filtra a linha e a operação afeta zero registros.
    /// A diferença importa — o repositório traduz "zero linhas" em "não encontrada", e é esse o
    /// resultado que o destinatário tem de ver ao tentar mexer na renda do dono.
    /// </summary>
    [Fact]
    public async Task Destinatario_nao_remove_nem_altera_a_renda_do_dono()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);
        var bruno = await CriarContaAsync(contexto);

        var fonteId = await DeclararRendaAsync(contexto, ana, "Salário", 6000m);

        await CompartilharAsync(contexto, ana, bruno);

        var fontes = new FonteRendaWriteRepository(ambiente.CriarGerenciador(contexto));

        // Ler pode: a fonte aparece para quem recebeu o compartilhamento.
        Assert.NotNull(await fontes.ObterAsync(fonteId, Ct));

        Assert.False(await fontes.RemoverAsync(fonteId, Ct));

        Assert.False(await fontes.AlterarAsync(
            new FonteRendaEntity
            {
                Id = fonteId,
                TenantId = ana.TenantId,
                Nome = "Renda inventada pelo Bruno",
                Valor = 1m,
                DiaRecebimento = 1,
                VigenciaInicio = Agosto,
                CriadoEm = DateTimeOffset.UtcNow,
            },
            Ct));

        // Nada mudou no tenant da Ana.
        Ativar(contexto, ana, somenteLeitura: false);

        var vigentes = await new FonteRendaReadRepository(ambiente.CriarGerenciador(contexto))
            .ListarAsync(Agosto, Ct);

        var fonte = Assert.Single(vigentes);

        Assert.Equal("Salário", fonte.Nome);
        Assert.Equal(6000m, fonte.Valor);
    }
}
