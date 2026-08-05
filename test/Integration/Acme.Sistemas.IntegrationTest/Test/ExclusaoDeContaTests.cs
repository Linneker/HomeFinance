using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.IntegrationTest.Config;
using Acme.Sistemas.Services.V1.Assinaturas;
using Moq;
using Acme.Sistemas.Repository.Repositories.V1.Tenant;
using Acme.Sistemas.Repository.Repositories.V1.Usuario;
using Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;
using Acme.Sistemas.Services.V1.Identidade.Command.ExcluirConta;

namespace Acme.Sistemas.IntegrationTest.Test;

/// <summary>
/// Exclusão de conta e o efeito imediato sobre quem recebia o tenant compartilhado.
/// </summary>
/// <remarks>
/// O caminho exercitado é o real — handler, repositório, policies — e não SQL montado no teste:
/// o que precisa ser provado é que a revogação e o DELETE acontecem na mesma transação e que
/// a resolução de acesso da requisição seguinte já enxerga o resultado. Um teste que montasse
/// o SQL por conta própria provaria o banco, não o sistema.
/// </remarks>
[Collection(nameof(BancoCollection))]
public sealed class ExclusaoDeContaTests(AmbienteBanco ambiente)
{
    /// <summary>
    /// Assinatura ativa com cota folgada. O que estes testes provam é o isolamento no banco; a
    /// cota é regra de aplicação e tem cobertura própria.
    /// </summary>
    private static IAssinaturaService ComCota()
    {
        var assinaturas = new Mock<IAssinaturaService>();

        assinaturas.Setup(a => a.SituacaoAtualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SituacaoDaConta(
                Acme.Sistemas.Domain.Enums.SituacaoAssinatura.Ativa, "casal", 3, 0));

        return assinaturas.Object;
    }

    private static CancellationToken Ct => CancellationToken.None;

    private sealed record Conta(Guid UsuarioId, Guid TenantId, string Email);

    /// <summary>Cria a conta pelo repositório real: usuário, tenant e consentimentos num commit.</summary>
    private async Task<Conta> CriarContaAsync(ContextoFixo contexto)
    {
        var usuarioId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var email = $"{usuarioId:N}@teste.local";
        var agora = DateTimeOffset.UtcNow;

        // Criação de conta acontece antes de existir usuário autenticado — é a única escrita
        // do schema nessa condição, e a policy de INSERT reflete isso.
        contexto.UsuarioId = null;
        contexto.TenantId = null;
        contexto.SomenteLeitura = false;

        var usuarios = new UsuarioWriteRepository(ambiente.CriarGerenciador(contexto));

        await usuarios.CriarContaAsync(
            new UsuarioEntity
            {
                Id = usuarioId,
                Email = email,
                SenhaHash = "hash-de-teste",
                CriadoEm = agora,
            },
            new TenantEntity { Id = tenantId, UsuarioId = usuarioId, CriadoEm = agora },
            DocumentoConsentimento.Obrigatorios
                .Select(documento => new ConsentimentoEntity
                {
                    Id = Guid.CreateVersion7(),
                    UsuarioId = usuarioId,
                    Documento = documento,
                    Versao = "1.0",
                    AceitoEm = agora,
                })
                .ToList(),
            Ct);

        return new Conta(usuarioId, tenantId, email);
    }

    /// <summary>Ana concede a Bruno, pelo Command real, o acesso somente leitura ao seu tenant.</summary>
    private async Task CompartilharAsync(ContextoFixo contexto, Conta ana, Conta bruno)
    {
        contexto.UsuarioId = ana.UsuarioId;
        contexto.TenantId = ana.TenantId;
        contexto.SomenteLeitura = false;

        var handler = new CompartilharTenantCommandHandler(
            new TenantWriteRepository(ambiente.CriarGerenciador(contexto)),
            ComCota(),
            TimeProvider.System);

        var resultado = await handler.HandleAsync(
            new CompartilharTenantCommand(
                ana.UsuarioId, ana.TenantId, bruno.Email,
                ConsentimentoConfirmado: true, IncluiScr: false),
            Ct);

        Assert.True(resultado.Sucesso);
    }

    private async Task ExcluirContaAsync(ContextoFixo contexto, Conta titular)
    {
        contexto.UsuarioId = titular.UsuarioId;
        contexto.TenantId = titular.TenantId;
        contexto.SomenteLeitura = false;

        var handler = new ExcluirContaCommandHandler(
            new UsuarioWriteRepository(ambiente.CriarGerenciador(contexto)));

        var resultado = await handler.HandleAsync(
            new ExcluirContaCommand(titular.UsuarioId, ConfirmacaoDoTitular: true), Ct);

        Assert.True(resultado.Sucesso);
    }

    private Task<TenantDisponivel?> ResolverAcessoAsync(ContextoFixo contexto, Guid usuarioId, Guid tenantId)
    {
        contexto.UsuarioId = usuarioId;
        contexto.TenantId = null;
        contexto.SomenteLeitura = true;

        return new TenantWriteRepository(ambiente.CriarGerenciador(contexto))
            .ResolverAcessoAsync(usuarioId, tenantId, Ct);
    }

    [Fact]
    public async Task Exclusao_de_conta_encerra_o_compartilhamento_e_o_destinatario_perde_o_acesso()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);
        var bruno = await CriarContaAsync(contexto);

        await CompartilharAsync(contexto, ana, bruno);

        var antes = await ResolverAcessoAsync(contexto, bruno.UsuarioId, ana.TenantId);

        Assert.NotNull(antes);
        Assert.False(antes.EhProprio);
        Assert.True(antes.SomenteLeitura);

        await ExcluirContaAsync(contexto, ana);

        // Sem espera, sem rotina de limpeza, sem invalidação de cache: a resolução seguinte já
        // não encontra acesso. É o que a revogação na mesma transação do DELETE garante.
        var depois = await ResolverAcessoAsync(contexto, bruno.UsuarioId, ana.TenantId);

        Assert.Null(depois);

        // A conta do Bruno é dele: a exclusão da Ana não pode alcançá-la.
        var proprio = await ResolverAcessoAsync(contexto, bruno.UsuarioId, bruno.TenantId);

        Assert.NotNull(proprio);
        Assert.True(proprio.EhProprio);
        Assert.False(proprio.SomenteLeitura);
    }

    [Fact]
    public async Task Destinatario_que_insista_no_tenant_excluido_nao_enxerga_linha_alguma()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);
        var bruno = await CriarContaAsync(contexto);

        await CompartilharAsync(contexto, ana, bruno);
        await SemearLancamentoAsync(contexto, ana);

        contexto.UsuarioId = bruno.UsuarioId;
        contexto.TenantId = ana.TenantId;
        contexto.SomenteLeitura = true;

        var visiveisAntes = await ContarLancamentosAsync(contexto);

        Assert.Equal(1, visiveisAntes);

        await ExcluirContaAsync(contexto, ana);

        // O cliente pode continuar mandando o X-Tenant da conta excluída — token velho, tela
        // aberta, retry. A resolução de acesso já recusaria, mas se algo a contornasse a RLS
        // ainda devolve vazio: são duas barreiras independentes, não uma.
        contexto.UsuarioId = bruno.UsuarioId;
        contexto.TenantId = ana.TenantId;
        contexto.SomenteLeitura = true;

        var visiveisDepois = await ContarLancamentosAsync(contexto);

        Assert.Equal(0, visiveisDepois);
    }

    private async Task SemearLancamentoAsync(ContextoFixo contexto, Conta dono)
    {
        contexto.UsuarioId = dono.UsuarioId;
        contexto.TenantId = dono.TenantId;
        contexto.SomenteLeitura = false;

        await using var escopo = await ambiente.CriarGerenciador(contexto).AbrirEscritaAsync(Ct);
        await using var comando = escopo.Conexao.CreateCommand();
        comando.Transaction = escopo.Transacao;
        comando.CommandText = """
            INSERT INTO lancamento (id, tenant_id, descricao, valor, data_competencia, data_caixa, criado_em)
            VALUES (@id, @tenant, 'Mercado', 240.00, DATE '2026-07-03', DATE '2026-08-05', now())
            """;

        Adicionar(comando, "id", Guid.CreateVersion7());
        Adicionar(comando, "tenant", dono.TenantId);

        await comando.ExecuteNonQueryAsync(Ct);
        await escopo.ConfirmarAsync(Ct);
    }

    private async Task<long> ContarLancamentosAsync(ContextoFixo contexto)
    {
        await using var escopo = await ambiente.CriarGerenciador(contexto).AbrirLeituraAsync(Ct);
        await using var comando = escopo.Conexao.CreateCommand();
        comando.Transaction = escopo.Transacao;
        comando.CommandText = "SELECT count(*) FROM lancamento";

        return Convert.ToInt64(await comando.ExecuteScalarAsync(Ct));
    }

    private static void Adicionar(DbCommand comando, string nome, object valor)
    {
        var parametro = comando.CreateParameter();
        parametro.ParameterName = nome;
        parametro.Value = valor;
        comando.Parameters.Add(parametro);
    }
}
