using System.Data.Common;
using Acme.Sistemas.IntegrationTest.Config;

namespace Acme.Sistemas.IntegrationTest.Test;

/// <summary>
/// Isolamento entre tenants imposto pelo banco. Estes testes nunca deveriam quebrar — e no dia
/// em que quebrarem, evitam um vazamento de dado financeiro entre clientes.
/// </summary>
[Collection(nameof(BancoCollection))]
public sealed class IsolamentoTenantTests(AmbienteBanco ambiente)
{
    private static CancellationToken Ct => CancellationToken.None;

    private sealed record Cenario(Guid UsuarioA, Guid TenantA, Guid UsuarioB, Guid TenantB, Guid CategoriaB);

    /// <summary>Cria duas contas com um lançamento cada, cada uma no seu tenant.</summary>
    private async Task<Cenario> SemearAsync()
    {
        var usuarioA = Guid.NewGuid();
        var tenantA = Guid.NewGuid();
        var usuarioB = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var categoriaB = Guid.NewGuid();

        var contexto = new ContextoFixo { SomenteLeitura = false };
        var gerenciador = ambiente.CriarGerenciador(contexto);

        foreach (var (usuario, tenant, categoria) in new[]
                 {
                     (usuarioA, tenantA, Guid.NewGuid()),
                     (usuarioB, tenantB, categoriaB),
                 })
        {
            contexto.UsuarioId = usuario;
            contexto.TenantId = null;

            await using (var escopo = await gerenciador.AbrirEscritaAsync(Ct))
            {
                await ExecutarAsync(escopo.Conexao, escopo.Transacao,
                    "INSERT INTO usuario (id, email, senha_hash, criado_em) VALUES (@id, @email, 'x', now())",
                    ("id", usuario), ("email", $"{usuario:N}@teste.local"));

                await ExecutarAsync(escopo.Conexao, escopo.Transacao,
                    "INSERT INTO tenant (id, usuario_id, criado_em) VALUES (@id, @usuario, now())",
                    ("id", tenant), ("usuario", usuario));

                await escopo.ConfirmarAsync(Ct);
            }

            contexto.TenantId = tenant;

            await using (var escopo = await gerenciador.AbrirEscritaAsync(Ct))
            {
                await ExecutarAsync(escopo.Conexao, escopo.Transacao,
                    "INSERT INTO categoria (id, tenant_id, nome, criado_em) VALUES (@id, @tenant, 'Alimentacao', now())",
                    ("id", categoria), ("tenant", tenant));

                await ExecutarAsync(escopo.Conexao, escopo.Transacao,
                    """
                    INSERT INTO lancamento (id, tenant_id, descricao, valor, data_competencia, data_caixa, categoria_id, criado_em)
                    VALUES (@id, @tenant, 'Mercado', 240.00, DATE '2026-07-03', DATE '2026-08-05', @categoria, now())
                    """,
                    ("id", Guid.NewGuid()), ("tenant", tenant), ("categoria", categoria));

                await escopo.ConfirmarAsync(Ct);
            }
        }

        return new Cenario(usuarioA, tenantA, usuarioB, tenantB, categoriaB);
    }

    [Fact]
    public async Task Tenant_nao_le_linha_de_outro_tenant()
    {
        var cenario = await SemearAsync();

        var contexto = new ContextoFixo
        {
            UsuarioId = cenario.UsuarioA,
            TenantId = cenario.TenantA,
            SomenteLeitura = false,
        };

        await using var escopo = await ambiente.CriarGerenciador(contexto).AbrirLeituraAsync(Ct);

        // Consulta SEM filtro de tenant — é exatamente o WHERE esquecido que a RLS existe
        // para neutralizar.
        var doOutroTenant = await ContarAsync(escopo.Conexao, escopo.Transacao,
            "SELECT count(*) FROM lancamento WHERE tenant_id = @tenant", ("tenant", cenario.TenantB));

        var visiveis = await ContarAsync(escopo.Conexao, escopo.Transacao,
            "SELECT count(*) FROM lancamento");

        Assert.Equal(0, doOutroTenant);
        Assert.Equal(1, visiveis);
    }

    [Fact]
    public async Task Escrita_em_tenant_alheio_nao_grava_linha()
    {
        var cenario = await SemearAsync();

        var contexto = new ContextoFixo
        {
            UsuarioId = cenario.UsuarioA,
            TenantId = cenario.TenantA,
            SomenteLeitura = false,
        };

        var gerenciador = ambiente.CriarGerenciador(contexto);

        await using (var escopo = await gerenciador.AbrirEscritaAsync(Ct))
        {
            // A policy de INSERT recusa a linha cujo tenant_id não é o ativo.
            await Assert.ThrowsAnyAsync<DbException>(async () =>
                await ExecutarAsync(escopo.Conexao, escopo.Transacao,
                    """
                    INSERT INTO lancamento (id, tenant_id, descricao, valor, data_competencia, data_caixa, criado_em)
                    VALUES (@id, @tenant, 'Invasao', 10.00, CURRENT_DATE, CURRENT_DATE, now())
                    """,
                    ("id", Guid.NewGuid()), ("tenant", cenario.TenantB)));
        }

        contexto.TenantId = cenario.TenantB;
        contexto.UsuarioId = cenario.UsuarioB;

        await using var verificacao = await gerenciador.AbrirLeituraAsync(Ct);

        var total = await ContarAsync(verificacao.Conexao, verificacao.Transacao,
            "SELECT count(*) FROM lancamento WHERE descricao = 'Invasao'");

        Assert.Equal(0, total);
    }

    [Fact]
    public async Task Contexto_somente_leitura_recusa_escrita_no_proprio_tenant()
    {
        var cenario = await SemearAsync();

        // É este cenário que impõe o compartilhamento read-only pelo banco, e não por
        // verificação espalhada nos handlers.
        var contexto = new ContextoFixo
        {
            UsuarioId = cenario.UsuarioA,
            TenantId = cenario.TenantB,
            SomenteLeitura = true,
        };

        await using var escopo = await ambiente.CriarGerenciador(contexto).AbrirEscritaAsync(Ct);

        var leu = await ContarAsync(escopo.Conexao, escopo.Transacao, "SELECT count(*) FROM lancamento");

        await Assert.ThrowsAnyAsync<DbException>(async () =>
            await ExecutarAsync(escopo.Conexao, escopo.Transacao,
                """
                INSERT INTO lancamento (id, tenant_id, descricao, valor, data_competencia, data_caixa, criado_em)
                VALUES (@id, @tenant, 'Escrita indevida', 10.00, CURRENT_DATE, CURRENT_DATE, now())
                """,
                ("id", Guid.NewGuid()), ("tenant", cenario.TenantB)));

        Assert.Equal(1, leu);
    }

    [Fact]
    public async Task Select_pela_conexao_de_escrita_enxerga_o_tenant_ativo()
    {
        var cenario = await SemearAsync();

        // O WriteRepository faz SELECT pela WriteConnection para decidir a escrita. Se o
        // contexto não valesse nessa conexão, a verificação de unicidade voltaria vazia e o
        // Command gravaria duplicata — falha silenciosa, dado errado.
        var contexto = new ContextoFixo
        {
            UsuarioId = cenario.UsuarioB,
            TenantId = cenario.TenantB,
            SomenteLeitura = false,
        };

        await using var escopo = await ambiente.CriarGerenciador(contexto).AbrirEscritaAsync(Ct);

        var existe = await ContarAsync(escopo.Conexao, escopo.Transacao,
            "SELECT count(*) FROM categoria WHERE id = @id", ("id", cenario.CategoriaB));

        Assert.Equal(1, existe);
    }

    [Fact]
    public async Task Sem_contexto_de_tenant_nenhuma_linha_e_visivel()
    {
        await SemearAsync();

        // É o caso do administrador: não tem tenant, então não enxerga dado financeiro de
        // cliente nenhum — sem mecanismo adicional, apenas pela ausência de contexto.
        var contexto = new ContextoFixo { UsuarioId = null, TenantId = null, SomenteLeitura = true };

        await using var escopo = await ambiente.CriarGerenciador(contexto).AbrirLeituraAsync(Ct);

        var lancamentos = await ContarAsync(escopo.Conexao, escopo.Transacao, "SELECT count(*) FROM lancamento");
        var usuarios = await ContarAsync(escopo.Conexao, escopo.Transacao, "SELECT count(*) FROM usuario");

        Assert.Equal(0, lancamentos);
        Assert.Equal(0, usuarios);
    }

    [Fact]
    public async Task Toda_tabela_com_tenant_id_tem_rls_habilitada_e_forcada()
    {
        var contexto = new ContextoFixo();

        await using var escopo = await ambiente.CriarGerenciador(contexto).AbrirLeituraAsync(Ct);

        await using var comando = escopo.Conexao.CreateCommand();
        comando.Transaction = escopo.Transacao;

        // Varredura do catálogo: uma tabela nova criada sem policy passaria despercebida em
        // revisão, mas não aqui.
        comando.CommandText = """
            SELECT string_agg(c.relname, ', ' ORDER BY c.relname)
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'public'
              AND c.relkind = 'r'
              AND EXISTS (
                  SELECT 1 FROM pg_attribute a
                  WHERE a.attrelid = c.oid AND a.attname = 'tenant_id' AND a.attnum > 0 AND NOT a.attisdropped
              )
              AND NOT (c.relrowsecurity AND c.relforcerowsecurity)
            """;

        var desprotegidas = await comando.ExecuteScalarAsync(Ct);

        Assert.True(
            desprotegidas is null or DBNull,
            $"Tabelas com tenant_id sem RLS habilitada e forçada: {desprotegidas}");
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

        await comando.ExecuteNonQueryAsync(CancellationToken.None);
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

        var resultado = await comando.ExecuteScalarAsync(CancellationToken.None);
        return Convert.ToInt64(resultado);
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
