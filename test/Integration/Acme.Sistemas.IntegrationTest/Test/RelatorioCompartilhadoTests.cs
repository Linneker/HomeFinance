using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Entities.Planejamento;
using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.IntegrationTest.Config;
using Acme.Sistemas.Repository.Repositories.V1.Categoria;
using Acme.Sistemas.Repository.Repositories.V1.Lancamento;
using Acme.Sistemas.Repository.Repositories.V1.Orcamento;
using Acme.Sistemas.Repository.Repositories.V1.Tenant;
using Acme.Sistemas.Repository.Repositories.V1.Usuario;
using Acme.Sistemas.Services.V1.Assinaturas;
using Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;
using Acme.Sistemas.Services.V1.Relatorios.Query.ExtratoDoMes;
using Acme.Sistemas.Services.V1.Relatorios.Query.ResumoDoMes;
using Moq;

namespace Acme.Sistemas.IntegrationTest.Test;

/// <summary>
/// Bruno seleciona o tenant da Ana e abre o relatório: vê os números da Ana, e só os dela.
/// </summary>
/// <remarks>
/// <see cref="RelatorioIsoladoTests"/> já prova que dois tenants não se somam, mas sempre com o
/// dono lendo o próprio espaço. O cenário da spec é outro: quem lê não é o dono, e as
/// participações que sustentam a apuração são de outra pessoa.
/// <para>
/// A distinção é o que este teste protege. A consulta do relatório não recebe tenant — o recorte
/// vem da RLS, por <c>app_tenant_id()</c>. Uma policy escrita por <c>app_usuario_id()</c>
/// isolaria igualmente bem entre famílias e passaria em todo teste de isolamento existente, mas
/// devolveria zero linhas aqui: as participações são da Ana, e quem está lendo é o Bruno.
/// </para>
/// </remarks>
[Collection(nameof(BancoCollection))]
public sealed class RelatorioCompartilhadoTests(AmbienteBanco ambiente)
{
    private static CancellationToken Ct => CancellationToken.None;

    private static readonly DateOnly Agosto = new(2026, 8, 1);

    private sealed record Conta(Guid UsuarioId, Guid TenantId, string Email);

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

    /// <summary>
    /// Lançamento e participação de 100% na mesma transação — o único estado em que a linha é
    /// apurável. A participação fica no nome do dono do tenant.
    /// </summary>
    private async Task LancarAsync(
        ContextoFixo contexto, Conta dono, string descricao, decimal valor, Guid categoriaId)
    {
        Ativar(contexto, dono, somenteLeitura: false);

        var lancamento = new LancamentoEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = dono.TenantId,
            Descricao = descricao,
            Valor = valor,
            DataCompetencia = Agosto,
            DataCaixa = Agosto,
            CategoriaId = categoriaId,
            CriadoEm = DateTimeOffset.UtcNow,
        };

        await new LancamentoWriteRepository(ambiente.CriarGerenciador(contexto)).CriarAsync(
            [lancamento],
            [
                new LancamentoParticipacaoEntity
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = dono.TenantId,
                    LancamentoId = lancamento.Id,
                    UsuarioId = dono.UsuarioId,
                    Percentual = LancamentoParticipacaoEntity.PercentualIntegral,
                    CriadoEm = DateTimeOffset.UtcNow,
                },
            ],
            Ct);
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
    /// Concede pelo Command real e deixa o contexto no estado que o middleware produziria ao
    /// selecionar o tenant compartilhado.
    /// </summary>
    private async Task SelecionarTenantCompartilhadoAsync(
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
    }

    private ExtratoDoMesQueryHandler Extrato(ContextoFixo contexto)
    {
        var gerenciador = ambiente.CriarGerenciador(contexto);

        return new ExtratoDoMesQueryHandler(
            new RelatorioReadRepository(gerenciador), new OrcamentoReadRepository(gerenciador));
    }

    private ResumoDoMesQueryHandler Resumo(ContextoFixo contexto)
    {
        var gerenciador = ambiente.CriarGerenciador(contexto);

        return new ResumoDoMesQueryHandler(
            new RelatorioReadRepository(gerenciador), new OrcamentoReadRepository(gerenciador));
    }

    /// <summary>
    /// Cenário literal da spec. Os dois gastaram no mesmo mês, na mesma categoria pelo nome e com
    /// valores diferentes — se o recorte falhar, o total denuncia qual dos dois vazou.
    /// </summary>
    [Fact]
    public async Task Extrato_do_tenant_compartilhado_traz_so_os_lancamentos_do_dono()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);
        var bruno = await CriarContaAsync(contexto);

        var alimentacaoDaAna = await CriarCategoriaAsync(contexto, ana, "Alimentação");
        var alimentacaoDoBruno = await CriarCategoriaAsync(contexto, bruno, "Alimentação");

        await LancarAsync(contexto, ana, "Mercado", 980m, alimentacaoDaAna);
        await LancarAsync(contexto, bruno, "Mercado", 333m, alimentacaoDoBruno);

        await SelecionarTenantCompartilhadoAsync(contexto, ana, bruno);

        var resultado = await Extrato(contexto)
            .HandleAsync(new ExtratoDoMesQuery(ana.TenantId, Agosto), Ct);

        var linha = Assert.Single(resultado.Lancamentos);

        Assert.Equal("Mercado", linha.Descricao);

        // `Valor` no extrato já é o apurado do tenant, não o valor cheio do lançamento.
        Assert.Equal(980m, linha.Valor);
        Assert.Equal(alimentacaoDaAna, linha.CategoriaId);
        Assert.Equal(980m, resultado.Total);
        Assert.False(resultado.Vazio);
    }

    /// <summary>
    /// O resumo cruza duas tabelas com recorte próprio — <c>lancamento_participacao</c> e
    /// <c>orcamento_mensal</c>. Se só uma delas seguisse o tenant selecionado, o planejado e o
    /// realizado viriam de espaços diferentes e a diferença sairia errada sem nada estourar.
    /// </summary>
    [Fact]
    public async Task Resumo_do_tenant_compartilhado_cruza_planejado_e_realizado_do_dono()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);
        var bruno = await CriarContaAsync(contexto);

        var alimentacaoDaAna = await CriarCategoriaAsync(contexto, ana, "Alimentação");
        var alimentacaoDoBruno = await CriarCategoriaAsync(contexto, bruno, "Alimentação");

        await OrcarAsync(contexto, ana, alimentacaoDaAna, 1200m);
        await LancarAsync(contexto, ana, "Mercado", 980m, alimentacaoDaAna);

        await OrcarAsync(contexto, bruno, alimentacaoDoBruno, 7m);
        await LancarAsync(contexto, bruno, "Mercado", 333m, alimentacaoDoBruno);

        await SelecionarTenantCompartilhadoAsync(contexto, ana, bruno);

        var resultado = await Resumo(contexto)
            .HandleAsync(new ResumoDoMesQuery(ana.TenantId, Agosto), Ct);

        var linha = Assert.Single(resultado.Categorias);

        Assert.Equal(alimentacaoDaAna, linha.CategoriaId);
        Assert.Equal(1200m, linha.Planejado);
        Assert.Equal(980m, linha.Realizado);
        Assert.Equal(220m, linha.Disponivel);
        Assert.False(linha.Excedida);

        Assert.Empty(resultado.ForaDoPlanejamento.Categorias);
        Assert.Equal(220m, resultado.Diferenca);
    }

    /// <summary>
    /// A volta: com o próprio tenant selecionado, o Bruno vê o dele e nada da Ana. Sem esta
    /// metade, um recorte quebrado que devolvesse tudo para todos passaria no teste anterior
    /// sempre que a Ana fosse a única com lançamento.
    /// </summary>
    [Fact]
    public async Task De_volta_ao_proprio_tenant_o_destinatario_ve_apenas_os_proprios_numeros()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);
        var bruno = await CriarContaAsync(contexto);

        var alimentacaoDaAna = await CriarCategoriaAsync(contexto, ana, "Alimentação");
        var alimentacaoDoBruno = await CriarCategoriaAsync(contexto, bruno, "Alimentação");

        await LancarAsync(contexto, ana, "Mercado", 980m, alimentacaoDaAna);
        await LancarAsync(contexto, bruno, "Mercado", 333m, alimentacaoDoBruno);

        await SelecionarTenantCompartilhadoAsync(contexto, ana, bruno);

        // Troca de tenant: o próprio volta a ser de escrita.
        Ativar(contexto, bruno, somenteLeitura: false);

        var resultado = await Extrato(contexto)
            .HandleAsync(new ExtratoDoMesQuery(bruno.TenantId, Agosto), Ct);

        var linha = Assert.Single(resultado.Lancamentos);

        Assert.Equal(333m, linha.Valor);
        Assert.Equal(alimentacaoDoBruno, linha.CategoriaId);
    }
}
