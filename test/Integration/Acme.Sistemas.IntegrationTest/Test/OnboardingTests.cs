using Acme.Sistemas.Domain.Entities.Onboarding;
using Acme.Sistemas.Domain.Entities.Planejamento;
using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.IntegrationTest.Config;
using Acme.Sistemas.Repository.Repositories.V1.FonteRenda;
using Acme.Sistemas.Repository.Repositories.V1.Lancamento;
using Acme.Sistemas.Repository.Repositories.V1.Onboarding;
using Acme.Sistemas.Repository.Repositories.V1.Usuario;

namespace Acme.Sistemas.IntegrationTest.Test;

/// <summary>
/// Onboarding contra o banco real: catálogo semeado, estado por tenant e leitura liberada.
/// </summary>
/// <remarks>
/// O ponto que precisa de integração é o isolamento do estado. O bloqueio em si é decidido em
/// memória e já tem teste unitário; o que só o banco prova é que o cumprimento de um tenant não
/// aparece no outro, e que a leitura de um tenant compartilhado não depende do onboarding de
/// quem está olhando.
/// </remarks>
[Collection(nameof(BancoCollection))]
public sealed class OnboardingTests(AmbienteBanco ambiente)
{
    private static CancellationToken Ct => CancellationToken.None;

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

    private OnboardingWriteRepository Onboarding(ContextoFixo contexto) =>
        new(ambiente.CriarGerenciador(contexto));

    private async Task<ProgressoDoOnboarding> ProgressoAsync(ContextoFixo contexto)
    {
        var repositorio = Onboarding(contexto);

        return ProgressoDoOnboarding.Montar(
            await repositorio.CatalogoAsync(Ct),
            await repositorio.EstadoDoTenantAsync(Ct));
    }

    /// <summary>
    /// O catálogo semeado pela migration é a configuração real do produto: renda ativa e
    /// bloqueante, os requisitos documentais declarados porém ainda inativos.
    /// </summary>
    [Fact]
    public async Task Catalogo_semeado_tem_a_renda_ativa_e_os_documentais_anunciados()
    {
        var contexto = new ContextoFixo();
        var ana = await CriarContaAsync(contexto);

        Ativar(contexto, ana, somenteLeitura: false);

        var catalogo = await Onboarding(contexto).CatalogoAsync(Ct);

        var renda = Assert.Single(
            catalogo,
            requisito => requisito.Codigo == OnboardingRequisitoEntity.CodigoRendaDeclarada);

        Assert.True(renda.BloqueiaAgora);
        Assert.Contains(catalogo, requisito => !requisito.Ativo);
        Assert.DoesNotContain(
            catalogo,
            requisito => requisito.Codigo != OnboardingRequisitoEntity.CodigoRendaDeclarada
                         && requisito.BloqueiaAgora);
    }

    [Fact]
    public async Task Conta_nova_comeca_com_o_requisito_bloqueante_pendente()
    {
        var contexto = new ContextoFixo();
        var ana = await CriarContaAsync(contexto);

        Ativar(contexto, ana, somenteLeitura: false);

        var progresso = await ProgressoAsync(contexto);

        Assert.False(progresso.Liberado);
        Assert.Equal(
            OnboardingRequisitoEntity.CodigoRendaDeclarada, progresso.Bloqueios[0].Codigo);
    }

    /// <summary>
    /// A navegação fica liberada desde o primeiro acesso: as consultas respondem com estado
    /// vazio, e não com erro, mesmo antes de o onboarding estar concluído.
    /// </summary>
    [Fact]
    public async Task Consulta_de_relatorio_responde_com_onboarding_pendente()
    {
        var contexto = new ContextoFixo();
        var ana = await CriarContaAsync(contexto);

        Ativar(contexto, ana, somenteLeitura: false);

        Assert.False((await ProgressoAsync(contexto)).Liberado);

        var extrato = await new RelatorioReadRepository(ambiente.CriarGerenciador(contexto))
            .ExtratoAsync(new PeriodoDoRelatorio(Agosto, FimDeAgosto, PorCaixa: false), Ct);

        Assert.Empty(extrato);
    }

    [Fact]
    public async Task Declarar_renda_cumpre_o_requisito_e_libera_a_conta()
    {
        var contexto = new ContextoFixo();
        var ana = await CriarContaAsync(contexto);

        Ativar(contexto, ana, somenteLeitura: false);

        await new FonteRendaWriteRepository(ambiente.CriarGerenciador(contexto)).GravarAsync(
            new FonteRendaEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = ana.TenantId,
                Nome = "Salário",
                Valor = 6000m,
                DiaRecebimento = 5,
                VigenciaInicio = Agosto,
                CriadoEm = DateTimeOffset.UtcNow,
            },
            Ct);

        await Onboarding(contexto).MarcarCumpridoAsync(
            new OnboardingEstadoEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = ana.TenantId,
                RequisitoCodigo = OnboardingRequisitoEntity.CodigoRendaDeclarada,
                CumpridoEm = DateTimeOffset.UtcNow,
            },
            Ct);

        var progresso = await ProgressoAsync(contexto);

        Assert.True(progresso.Liberado);
        Assert.Equal(100, progresso.PercentualConcluido);
    }

    /// <summary>
    /// Marcar duas vezes não move a data do primeiro cumprimento — ela é o registro de quando a
    /// conta destravou.
    /// </summary>
    [Fact]
    public async Task Cumprir_o_mesmo_requisito_de_novo_preserva_a_data_original()
    {
        var contexto = new ContextoFixo();
        var ana = await CriarContaAsync(contexto);

        Ativar(contexto, ana, somenteLeitura: false);

        var primeira = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);

        await Onboarding(contexto).MarcarCumpridoAsync(
            new OnboardingEstadoEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = ana.TenantId,
                RequisitoCodigo = OnboardingRequisitoEntity.CodigoRendaDeclarada,
                CumpridoEm = primeira,
            },
            Ct);

        await Onboarding(contexto).MarcarCumpridoAsync(
            new OnboardingEstadoEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = ana.TenantId,
                RequisitoCodigo = OnboardingRequisitoEntity.CodigoRendaDeclarada,
                CumpridoEm = primeira.AddDays(30),
            },
            Ct);

        var estado = Assert.Single(await Onboarding(contexto).EstadoDoTenantAsync(Ct));

        Assert.Equal(primeira, estado.CumpridoEm);
    }

    /// <summary>
    /// O onboarding é por tenant. Bruno com o próprio onboarding pendente enxerga normalmente o
    /// tenant que a Ana compartilhou — e o cumprimento da Ana não vira progresso do Bruno.
    /// </summary>
    [Fact]
    public async Task Onboarding_de_um_tenant_nao_vaza_para_o_outro()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);
        var bruno = await CriarContaAsync(contexto);

        Ativar(contexto, ana, somenteLeitura: false);

        await Onboarding(contexto).MarcarCumpridoAsync(
            new OnboardingEstadoEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = ana.TenantId,
                RequisitoCodigo = OnboardingRequisitoEntity.CodigoRendaDeclarada,
                CumpridoEm = DateTimeOffset.UtcNow,
            },
            Ct);

        Assert.True((await ProgressoAsync(contexto)).Liberado);

        Ativar(contexto, bruno, somenteLeitura: false);

        Assert.False((await ProgressoAsync(contexto)).Liberado);

        // Bruno lendo o tenant da Ana: o que ele vê é o estado dela, e a leitura acontece
        // mesmo com o onboarding dele pendente.
        contexto.UsuarioId = bruno.UsuarioId;
        contexto.TenantId = ana.TenantId;
        contexto.SomenteLeitura = true;

        var extrato = await new RelatorioReadRepository(ambiente.CriarGerenciador(contexto))
            .ExtratoAsync(new PeriodoDoRelatorio(Agosto, FimDeAgosto, PorCaixa: false), Ct);

        Assert.Empty(extrato);
        Assert.True((await ProgressoAsync(contexto)).Liberado);
    }
}
