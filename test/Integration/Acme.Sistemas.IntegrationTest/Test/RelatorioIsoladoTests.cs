using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.IntegrationTest.Config;
using Acme.Sistemas.Repository.Repositories.V1.Categoria;
using Acme.Sistemas.Repository.Repositories.V1.Lancamento;
using Acme.Sistemas.Repository.Repositories.V1.Usuario;

namespace Acme.Sistemas.IntegrationTest.Test;

/// <summary>
/// O relatório apura a partir das participações e nunca soma tenants distintos.
/// </summary>
/// <remarks>
/// Precisa ser teste de integração. O recorte por tenant não está no SQL do relatório — está na
/// RLS — e um teste com repositório dublê provaria só que o handler soma o que recebeu. Aqui
/// dois tenants gravam no mesmo mês, com a mesma descrição e o mesmo valor, e o relatório de um
/// deles é conferido contra o total que existiria se a barreira falhasse.
/// </remarks>
[Collection(nameof(BancoCollection))]
public sealed class RelatorioIsoladoTests(AmbienteBanco ambiente)
{
    private static CancellationToken Ct => CancellationToken.None;

    private static readonly DateOnly Julho = new(2026, 7, 1);
    private static readonly DateOnly FimDeJulho = new(2026, 7, 31);
    private static readonly DateOnly Agosto = new(2026, 8, 1);
    private static readonly DateOnly FimDeAgosto = new(2026, 8, 31);

    private sealed record Conta(Guid UsuarioId, Guid TenantId);

    private async Task<Conta> CriarContaAsync(ContextoFixo contexto)
    {
        var usuarioId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var agora = DateTimeOffset.UtcNow;

        contexto.UsuarioId = null;
        contexto.TenantId = null;
        contexto.SomenteLeitura = false;

        await new UsuarioWriteRepository(ambiente.CriarGerenciador(contexto)).CriarContaAsync(
            new UsuarioEntity
            {
                Id = usuarioId,
                Email = $"{usuarioId:N}@teste.local",
                SenhaHash = "hash-de-teste",
                CriadoEm = agora,
            },
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

        return new Conta(usuarioId, tenantId);
    }

    private void Ativar(ContextoFixo contexto, Conta conta, bool somenteLeitura)
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
    /// Grava pelo repositório real: lançamento e participação de 100% na mesma transação, que é
    /// o único estado em que o lançamento é apurável.
    /// </summary>
    private async Task LancarAsync(
        ContextoFixo contexto,
        Conta dono,
        string descricao,
        decimal valor,
        DateOnly competencia,
        DateOnly caixa,
        Guid? categoriaId)
    {
        Ativar(contexto, dono, somenteLeitura: false);

        var lancamento = new LancamentoEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = dono.TenantId,
            Descricao = descricao,
            Valor = valor,
            DataCompetencia = competencia,
            DataCaixa = caixa,
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

    private IRelatorioReadRepository Relatorios(ContextoFixo contexto, Conta comoQuem)
    {
        Ativar(contexto, comoQuem, somenteLeitura: true);
        return new RelatorioReadRepository(ambiente.CriarGerenciador(contexto));
    }

    [Fact]
    public async Task Extrato_de_um_tenant_nao_traz_lancamento_de_outro()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);
        var bruno = await CriarContaAsync(contexto);

        var alimentacaoDaAna = await CriarCategoriaAsync(contexto, ana, "Alimentação");

        // Mesmo mês, mesma descrição, mesmo valor: se a barreira falhar, o total dobra e o
        // teste não tem como confundir isso com outra causa.
        await LancarAsync(contexto, ana, "Mercado", 980m, Agosto, Agosto, alimentacaoDaAna);
        await LancarAsync(contexto, bruno, "Mercado", 980m, Agosto, Agosto, null);

        var extrato = await Relatorios(contexto, ana).ExtratoAsync(
            new PeriodoDoRelatorio(Agosto, FimDeAgosto, PorCaixa: false), Ct);

        var linha = Assert.Single(extrato);

        Assert.Equal(ana.TenantId, linha.TenantId);
        Assert.Equal(980m, linha.ValorApurado);
        Assert.Equal(alimentacaoDaAna, linha.CategoriaId);
    }

    [Fact]
    public async Task Totais_por_categoria_de_um_tenant_nao_somam_valores_de_outro()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);
        var bruno = await CriarContaAsync(contexto);

        var transporteDaAna = await CriarCategoriaAsync(contexto, ana, "Transporte");
        var transporteDoBruno = await CriarCategoriaAsync(contexto, bruno, "Transporte");

        await LancarAsync(contexto, ana, "Combustível", 520m, Agosto, Agosto, transporteDaAna);
        await LancarAsync(contexto, bruno, "Combustível", 999m, Agosto, Agosto, transporteDoBruno);

        var totais = await Relatorios(contexto, ana).TotaisPorCategoriaAsync(
            new PeriodoDoRelatorio(Agosto, FimDeAgosto, PorCaixa: false), Ct);

        var total = Assert.Single(totais);

        Assert.Equal(transporteDaAna, total.CategoriaId);
        Assert.Equal(520m, total.Realizado);
        Assert.Equal(1, total.Quantidade);
    }

    /// <summary>
    /// A mesma linha, dois meses diferentes conforme a coluna consultada. É o que sustenta as
    /// duas visões sem ramo condicional por tipo de lançamento.
    /// </summary>
    [Fact]
    public async Task Mesma_linha_cai_em_julho_na_competencia_e_em_agosto_no_caixa()
    {
        var contexto = new ContextoFixo();
        var ana = await CriarContaAsync(contexto);
        var categoria = await CriarCategoriaAsync(contexto, ana, "Casa");

        await LancarAsync(contexto, ana, "Geladeira 1/3", 100m, Julho, Agosto, categoria);

        var relatorios = Relatorios(contexto, ana);

        var julhoCompetencia = await relatorios.ExtratoAsync(
            new PeriodoDoRelatorio(Julho, FimDeJulho, PorCaixa: false), Ct);

        var julhoCaixa = await relatorios.ExtratoAsync(
            new PeriodoDoRelatorio(Julho, FimDeJulho, PorCaixa: true), Ct);

        var agostoCaixa = await relatorios.ExtratoAsync(
            new PeriodoDoRelatorio(Agosto, FimDeAgosto, PorCaixa: true), Ct);

        Assert.Single(julhoCompetencia);
        Assert.Empty(julhoCaixa);
        Assert.Single(agostoCaixa);
    }

    /// <summary>
    /// O valor do relatório vem do percentual da participação, não do valor cheio. Com uma
    /// participação de 50%, o apurado tem de ser a metade — é a prova de que a apuração já está
    /// preparada para o rateio entre pessoas.
    /// </summary>
    [Fact]
    public async Task Valor_apurado_segue_o_percentual_da_participacao()
    {
        var contexto = new ContextoFixo();
        var ana = await CriarContaAsync(contexto);
        var categoria = await CriarCategoriaAsync(contexto, ana, "Dividido");

        Ativar(contexto, ana, somenteLeitura: false);

        var lancamento = new LancamentoEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = ana.TenantId,
            Descricao = "Jantar dividido",
            Valor = 200m,
            DataCompetencia = Agosto,
            DataCaixa = Agosto,
            CategoriaId = categoria,
            CriadoEm = DateTimeOffset.UtcNow,
        };

        await new LancamentoWriteRepository(ambiente.CriarGerenciador(contexto)).CriarAsync(
            [lancamento],
            [
                new LancamentoParticipacaoEntity
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = ana.TenantId,
                    LancamentoId = lancamento.Id,
                    UsuarioId = ana.UsuarioId,
                    Percentual = 50m,
                    CriadoEm = DateTimeOffset.UtcNow,
                },
            ],
            Ct);

        var extrato = await Relatorios(contexto, ana).ExtratoAsync(
            new PeriodoDoRelatorio(Agosto, FimDeAgosto, PorCaixa: false), Ct);

        var linha = Assert.Single(extrato);

        Assert.Equal(200m, linha.Valor);
        Assert.Equal(100m, linha.ValorApurado);
    }
}
