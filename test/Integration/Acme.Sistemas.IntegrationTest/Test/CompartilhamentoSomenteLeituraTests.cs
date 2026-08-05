using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.IntegrationTest.Config;
using Acme.Sistemas.Services.V1.Assinaturas;
using Moq;
using Acme.Sistemas.Repository.Repositories.V1.Tenant;
using Acme.Sistemas.Repository.Repositories.V1.Usuario;
using Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;

namespace Acme.Sistemas.IntegrationTest.Test;

/// <summary>
/// O compartilhamento é somente leitura, e quem impõe isso é o banco.
/// </summary>
/// <remarks>
/// O modo de acesso não é escolhido pelo teste: sai da resolução real do compartilhamento, o
/// mesmo caminho que o middleware percorre. Um teste que fixasse <c>SomenteLeitura = true</c>
/// por conta própria provaria a policy, mas não provaria que a concessão produz esse modo — e
/// é a ligação entre as duas coisas que pode quebrar.
/// </remarks>
[Collection(nameof(BancoCollection))]
public sealed class CompartilhamentoSomenteLeituraTests(AmbienteBanco ambiente)
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
            [
                new ConsentimentoEntity
                {
                    Id = Guid.CreateVersion7(),
                    UsuarioId = usuarioId,
                    Documento = DocumentoConsentimento.TermosDeUso,
                    Versao = "1.0",
                    AceitoEm = agora,
                },
            ],
            Ct);

        return new Conta(usuarioId, tenantId, email);
    }

    [Fact]
    public async Task Destinatario_com_leitura_tem_a_escrita_recusada_pelo_banco()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);
        var bruno = await CriarContaAsync(contexto);

        contexto.UsuarioId = ana.UsuarioId;
        contexto.TenantId = ana.TenantId;
        contexto.SomenteLeitura = false;

        await SemearCategoriaAsync(contexto, ana.TenantId, "Alimentacao");

        var concessao = await new CompartilharTenantCommandHandler(
            new TenantWriteRepository(ambiente.CriarGerenciador(contexto)),
            ComCota(),
            TimeProvider.System)
            .HandleAsync(
                new CompartilharTenantCommand(
                    ana.UsuarioId, ana.TenantId, bruno.Email,
                    ConsentimentoConfirmado: true, IncluiScr: false),
                Ct);

        Assert.True(concessao.Sucesso);

        // O modo vem da resolução do compartilhamento, não de uma escolha do teste.
        contexto.UsuarioId = bruno.UsuarioId;
        contexto.TenantId = null;
        contexto.SomenteLeitura = true;

        var acesso = await new TenantWriteRepository(ambiente.CriarGerenciador(contexto))
            .ResolverAcessoAsync(bruno.UsuarioId, ana.TenantId, Ct);

        Assert.NotNull(acesso);
        Assert.False(acesso.EhProprio);
        Assert.True(acesso.SomenteLeitura);

        contexto.UsuarioId = bruno.UsuarioId;
        contexto.TenantId = acesso.TenantId;
        contexto.SomenteLeitura = acesso.SomenteLeitura;

        await using (var escopo = await ambiente.CriarGerenciador(contexto).AbrirEscritaAsync(Ct))
        {
            // Ler pode: é para isso que o acesso foi concedido.
            var visiveis = await ContarAsync(escopo, "SELECT count(*) FROM categoria");

            Assert.Equal(1, visiveis);

            // Escrever não. Não é o handler que recusa — é a policy. Mesmo que um Command
            // novo esquecesse a verificação, a linha não entra.
            await Assert.ThrowsAnyAsync<DbException>(async () =>
                await ExecutarAsync(escopo,
                    """
                    INSERT INTO categoria (id, tenant_id, nome, criado_em)
                    VALUES (@id, @tenant, 'Criada por quem so le', now())
                    """,
                    ("id", Guid.CreateVersion7()),
                    ("tenant", ana.TenantId)));
        }

        // A recusa não deixou resíduo no tenant da Ana.
        contexto.UsuarioId = ana.UsuarioId;
        contexto.TenantId = ana.TenantId;
        contexto.SomenteLeitura = false;

        await using var verificacao = await ambiente.CriarGerenciador(contexto).AbrirLeituraAsync(Ct);

        var gravadas = await ContarAsync(verificacao, "SELECT count(*) FROM categoria");

        Assert.Equal(1, gravadas);
    }

    private async Task SemearCategoriaAsync(ContextoFixo contexto, Guid tenantId, string nome)
    {
        await using var escopo = await ambiente.CriarGerenciador(contexto).AbrirEscritaAsync(Ct);

        await ExecutarAsync(escopo,
            "INSERT INTO categoria (id, tenant_id, nome, criado_em) VALUES (@id, @tenant, @nome, now())",
            ("id", Guid.CreateVersion7()), ("tenant", tenantId), ("nome", nome));

        await escopo.ConfirmarAsync(Ct);
    }

    private static async Task ExecutarAsync(
        EscopoTransacao escopo, string sql, params (string Nome, object Valor)[] parametros)
    {
        await using var comando = Comando(escopo, sql, parametros);
        await comando.ExecuteNonQueryAsync(Ct);
    }

    private static async Task<long> ContarAsync(EscopoTransacao escopo, string sql)
    {
        await using var comando = Comando(escopo, sql, []);
        return Convert.ToInt64(await comando.ExecuteScalarAsync(Ct));
    }

    private static DbCommand Comando(
        EscopoTransacao escopo, string sql, (string Nome, object Valor)[] parametros)
    {
        var comando = escopo.Conexao.CreateCommand();
        comando.Transaction = escopo.Transacao;
        comando.CommandText = sql;

        foreach (var (nome, valor) in parametros)
        {
            var parametro = comando.CreateParameter();
            parametro.ParameterName = nome;
            parametro.Value = valor;
            comando.Parameters.Add(parametro);
        }

        return comando;
    }
}
