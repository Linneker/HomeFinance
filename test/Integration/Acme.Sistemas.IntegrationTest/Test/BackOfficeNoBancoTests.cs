using System.Data.Common;
using Acme.Sistemas.Core.Security;
using Acme.Sistemas.Domain.Entities.Administracao;
using Acme.Sistemas.Domain.Entities.Auditoria;
using Acme.Sistemas.Domain.Entities.Exportacao;
using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.IntegrationTest.Config;
using Acme.Sistemas.Repository.Repositories.V1.Administracao;
using Acme.Sistemas.Repository.Repositories.V1.Auditoria;
using Acme.Sistemas.Repository.Repositories.V1.Exportacao;
using Acme.Sistemas.Repository.Repositories.V1.Usuario;
using Acme.Sistemas.Services.V1.Administracao.Command.AutenticarAdministrador;
using Acme.Sistemas.Services.V1.Identidade.Command.Autenticar;

namespace Acme.Sistemas.IntegrationTest.Test;

/// <summary>
/// O back-office contra o banco: a identidade administrativa é à parte da do usuário final e não
/// enxerga dado financeiro de cliente, nem depois de a exportação ser habilitada.
/// </summary>
/// <remarks>
/// Precisa ser de integração. O isolamento é RLS e grant do PostgreSQL — o administrador não
/// seleciona tenant, então as tabelas financeiras devolvem vazio por construção — e a separação
/// das credenciais é a função <c>SECURITY DEFINER</c> de cada lado buscando na sua própria
/// tabela. Um dublê provaria só o que o handler faz com o que recebeu.
/// </remarks>
[Collection(nameof(BancoCollection))]
public sealed class BackOfficeNoBancoTests(AmbienteBanco ambiente)
{
    private static CancellationToken Ct => CancellationToken.None;

    private sealed record Conta(Guid UsuarioId, Guid TenantId, string Email, string Senha);

    /// <summary>Gerador de token que não assina de verdade: o que está sob teste é a recusa/aceite da credencial.</summary>
    private sealed class GeradorFalso : IGeradorDeToken
    {
        public Task<TokenEmitido> EmitirAsync(Guid usuarioId, Guid tenantPadraoId, CancellationToken _) =>
            Task.FromResult(new TokenEmitido("token-de-teste", DateTimeOffset.UtcNow.AddHours(1)));

        public Task<TokenEmitido> EmitirAdministradorAsync(Guid administradorId, CancellationToken _) =>
            Task.FromResult(new TokenEmitido("token-admin-de-teste", DateTimeOffset.UtcNow.AddHours(1)));
    }

    private async Task<Conta> CriarContaAsync(ContextoFixo contexto, string senha)
    {
        var usuarioId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var email = $"{usuarioId:N}@cliente.local";
        var agora = DateTimeOffset.UtcNow;

        contexto.UsuarioId = null;
        contexto.TenantId = null;
        contexto.SomenteLeitura = false;

        await new UsuarioWriteRepository(ambiente.CriarGerenciador(contexto)).CriarContaAsync(
            new UsuarioEntity
            {
                Id = usuarioId,
                Email = email,
                SenhaHash = Hash.GerarHashSenha(senha),
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

        return new Conta(usuarioId, tenantId, email, senha);
    }

    private async Task<Guid> ProvisionarAdministradorAsync(string email, string senha)
    {
        var administradorId = Guid.CreateVersion7();

        // Provisionamento fora de contexto: a função definer é o único caminho de escrita.
        var contexto = new ContextoFixo { UsuarioId = null, TenantId = null, SomenteLeitura = false };

        await new AdministradorRepository(ambiente.CriarGerenciador(contexto)).ProvisionarAsync(
            new AdministradorEntity
            {
                Id = administradorId,
                Email = email,
                Nome = "Operação",
                SenhaHash = string.Empty,
                CriadoEm = DateTimeOffset.UtcNow,
            },
            Hash.GerarHashSenha(senha),
            Ct);

        return administradorId;
    }

    // ------------------------------------------------------------ 14.8

    /// <summary>
    /// Autenticado como administrador, com a exportação já habilitada para uma conta que tem
    /// dado financeiro, a leitura das tabelas financeiras devolve zero linhas.
    /// </summary>
    [Fact]
    public async Task Administrador_nao_le_nenhuma_linha_de_tabela_financeira()
    {
        var contexto = new ContextoFixo();
        var conta = await CriarContaAsync(contexto, "senha-do-cliente");
        var administradorId = await ProvisionarAdministradorAsync("op@homefinance.local", "senha-do-admin");

        // A conta tem dado financeiro: uma categoria e um lançamento no próprio tenant.
        contexto.UsuarioId = conta.UsuarioId;
        contexto.TenantId = conta.TenantId;
        contexto.SomenteLeitura = false;

        var categoriaId = Guid.CreateVersion7();
        await using (var escopo = await ambiente.CriarGerenciador(contexto).AbrirEscritaAsync(Ct))
        {
            await ExecutarAsync(escopo.Conexao, escopo.Transacao,
                "INSERT INTO categoria (id, tenant_id, nome, criado_em) VALUES (@id, @tenant, 'Alimentacao', now())",
                ("id", categoriaId), ("tenant", conta.TenantId));

            await ExecutarAsync(escopo.Conexao, escopo.Transacao,
                """
                INSERT INTO lancamento (id, tenant_id, descricao, valor, data_competencia, data_caixa, categoria_id, criado_em)
                VALUES (@id, @tenant, 'Mercado', 240.00, DATE '2026-07-03', DATE '2026-08-05', @categoria, now())
                """,
                ("id", Guid.CreateVersion7()), ("tenant", conta.TenantId), ("categoria", categoriaId));

            await escopo.ConfirmarAsync(Ct);
        }

        // A operação habilita a exportação para a conta — é o "inclusive após habilitar" da spec.
        var contextoHabilitacao = new ContextoFixo { UsuarioId = administradorId, TenantId = null, SomenteLeitura = false };
        await new ExportacaoHabilitacaoWriteRepository(ambiente.CriarGerenciador(contextoHabilitacao)).DefinirAsync(
            new ExportacaoHabilitacaoEntity
            {
                Id = Guid.CreateVersion7(),
                UsuarioId = conta.UsuarioId,
                Habilitada = true,
                Motivo = "Requisição de acesso do titular",
                AutorId = administradorId,
                CriadoEm = DateTimeOffset.UtcNow,
            },
            Ct);

        // Contexto administrativo: o id do administrador como usuário, e NENHUM tenant — como o
        // token administrativo produz, por não carregar a claim de tenant.
        var contextoAdmin = new ContextoFixo
        {
            UsuarioId = administradorId,
            TenantId = null,
            SomenteLeitura = true,
        };

        await using var escopoAdmin = await ambiente.CriarGerenciador(contextoAdmin).AbrirLeituraAsync(Ct);

        foreach (var tabela in new[] { "lancamento", "categoria", "orcamento_mensal", "alerta", "regra_classificacao" })
        {
            var linhas = await ContarAsync(escopoAdmin.Conexao, escopoAdmin.Transacao, $"SELECT count(*) FROM {tabela}");
            Assert.Equal(0, linhas);
        }

        // A confirmação de que o dado existe: sob o contexto da conta, o lançamento aparece.
        var contextoConta = new ContextoFixo { UsuarioId = conta.UsuarioId, TenantId = conta.TenantId, SomenteLeitura = true };
        await using var escopoConta = await ambiente.CriarGerenciador(contextoConta).AbrirLeituraAsync(Ct);

        var doTitular = await ContarAsync(escopoConta.Conexao, escopoConta.Transacao, "SELECT count(*) FROM lancamento");
        Assert.Equal(1, doTitular);
    }

    /// <summary>
    /// O histórico de habilitação da conta é lido pela função definer, do mais recente ao mais
    /// antigo — é o que a tela administrativa da exportação mostra.
    /// </summary>
    [Fact]
    public async Task Historico_de_habilitacao_e_lido_do_mais_recente_ao_mais_antigo()
    {
        var contexto = new ContextoFixo();
        var conta = await CriarContaAsync(contexto, "senha-do-cliente");
        var administradorId = await ProvisionarAdministradorAsync("op4@homefinance.local", "senha-do-admin");

        var contextoAudit = new ContextoFixo { UsuarioId = administradorId, TenantId = null, SomenteLeitura = false };
        var auditoria = new AuditoriaRepository(ambiente.CriarGerenciador(contextoAudit));

        await auditoria.RegistrarAsync(
            new RegistroAuditoriaEntity
            {
                Id = Guid.CreateVersion7(),
                Evento = EventoAuditoria.ExportacaoHabilitada,
                ContaUsuarioId = conta.UsuarioId,
                AutorId = administradorId,
                Detalhe = "Requisição de acesso",
                CriadoEm = DateTimeOffset.UtcNow.AddMinutes(-10),
            },
            Ct);

        await auditoria.RegistrarAsync(
            new RegistroAuditoriaEntity
            {
                Id = Guid.CreateVersion7(),
                Evento = EventoAuditoria.ExportacaoDesabilitada,
                ContaUsuarioId = conta.UsuarioId,
                AutorId = administradorId,
                Detalhe = "Encerramento a pedido",
                CriadoEm = DateTimeOffset.UtcNow,
            },
            Ct);

        var historico = await new AuditoriaReadRepository(ambiente.CriarGerenciador(contextoAudit))
            .HistoricoExportacaoAsync(conta.UsuarioId, Ct);

        Assert.Equal(2, historico.Count);
        Assert.Equal(EventoAuditoria.ExportacaoDesabilitada, historico[0].Evento);
        Assert.Equal("Encerramento a pedido", historico[0].Detalhe);
        Assert.Equal(EventoAuditoria.ExportacaoHabilitada, historico[1].Evento);
    }

    // ------------------------------------------------------------ 14.9

    /// <summary>A credencial de administrador não autentica no login de usuário final.</summary>
    [Fact]
    public async Task Credencial_de_administrador_nao_autentica_como_usuario_final()
    {
        await ProvisionarAdministradorAsync("op2@homefinance.local", "senha-do-admin");

        var contexto = new ContextoFixo();
        var handler = new AutenticarCommandHandler(
            new UsuarioWriteRepository(ambiente.CriarGerenciador(contexto)),
            new GeradorFalso());

        var resultado = await handler.HandleAsync(
            new AutenticarCommand("op2@homefinance.local", "senha-do-admin"),
            Ct);

        Assert.False(resultado.Sucesso);
        Assert.Equal(AutenticarCommandHandler.CodigoCredencialInvalida, resultado.Erros[0].Codigo);
    }

    /// <summary>A credencial de usuário final não autentica no login administrativo.</summary>
    [Fact]
    public async Task Credencial_de_usuario_final_nao_autentica_como_administrador()
    {
        var contexto = new ContextoFixo();
        var conta = await CriarContaAsync(contexto, "senha-do-cliente");

        var handler = new AutenticarAdministradorCommandHandler(
            new AdministradorRepository(ambiente.CriarGerenciador(contexto)),
            new GeradorFalso());

        var resultado = await handler.HandleAsync(
            new AutenticarAdministradorCommand(conta.Email, conta.Senha),
            Ct);

        Assert.False(resultado.Sucesso);
        Assert.Equal(AutenticarAdministradorCommandHandler.CodigoCredencialInvalida, resultado.Erros[0].Codigo);
    }

    /// <summary>Cada credencial autentica no seu próprio lado — a separação não fecha o caminho legítimo.</summary>
    [Fact]
    public async Task Cada_credencial_autentica_no_proprio_lado()
    {
        var contexto = new ContextoFixo();
        var conta = await CriarContaAsync(contexto, "senha-do-cliente");
        await ProvisionarAdministradorAsync("op3@homefinance.local", "senha-do-admin");

        var usuarioFinal = await new AutenticarCommandHandler(
                new UsuarioWriteRepository(ambiente.CriarGerenciador(contexto)),
                new GeradorFalso())
            .HandleAsync(new AutenticarCommand(conta.Email, conta.Senha), Ct);

        var administrador = await new AutenticarAdministradorCommandHandler(
                new AdministradorRepository(ambiente.CriarGerenciador(contexto)),
                new GeradorFalso())
            .HandleAsync(new AutenticarAdministradorCommand("op3@homefinance.local", "senha-do-admin"), Ct);

        Assert.True(usuarioFinal.Sucesso);
        Assert.True(administrador.Sucesso);
    }

    private static async Task ExecutarAsync(
        DbConnection conexao,
        DbTransaction transacao,
        string sql,
        params (string Nome, object Valor)[] parametros)
    {
        await using var comando = conexao.CreateCommand();
        comando.Transaction = transacao;
        comando.CommandText = sql;
        Adicionar(comando, parametros);

        await comando.ExecuteNonQueryAsync(Ct);
    }

    private static async Task<long> ContarAsync(
        DbConnection conexao,
        DbTransaction transacao,
        string sql,
        params (string Nome, object Valor)[] parametros)
    {
        await using var comando = conexao.CreateCommand();
        comando.Transaction = transacao;
        comando.CommandText = sql;
        Adicionar(comando, parametros);

        return Convert.ToInt64(await comando.ExecuteScalarAsync(Ct));
    }

    private static void Adicionar(DbCommand comando, (string Nome, object Valor)[] parametros)
    {
        foreach (var (nome, valor) in parametros)
        {
            var parametro = comando.CreateParameter();
            parametro.ParameterName = nome;
            parametro.Value = valor;
            comando.Parameters.Add(parametro);
        }
    }
}
