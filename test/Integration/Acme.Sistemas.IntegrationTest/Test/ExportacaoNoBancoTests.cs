using Acme.Sistemas.Domain.Entities.Exportacao;
using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Storage;
using Acme.Sistemas.IntegrationTest.Config;
using Acme.Sistemas.Repository.Repositories.V1.Exportacao;
using Acme.Sistemas.Repository.Repositories.V1.Usuario;
using Acme.Sistemas.Services.V1.Exportacao;
using Acme.Sistemas.Services.V1.Exportacao.Query.BaixarExportacao;
using Acme.Sistemas.Domain.Entities.Auditoria;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Acme.Sistemas.IntegrationTest.Test;

/// <summary>
/// A exportação contra o banco: a habilitação não é escrita direto pelo app (só pela função
/// definer que o endpoint administrativo aciona), e a expiração remove o arquivo e fecha o
/// download — inclusive o download por outra conta.
/// </summary>
/// <remarks>
/// Precisa ser de integração. O bloqueio de escrita direta é grant e RLS do PostgreSQL, não
/// verificação de aplicação; e a varredura de expiração roda sem tenant, por função
/// <c>SECURITY DEFINER</c> — um dublê provaria só o que o handler faz com o que recebeu.
/// </remarks>
[Collection(nameof(BancoCollection))]
public sealed class ExportacaoNoBancoTests(AmbienteBanco ambiente)
{
    private static CancellationToken Ct => CancellationToken.None;

    private sealed record Conta(Guid UsuarioId, Guid TenantId);

    /// <summary>Auditoria no-op: o que está sob teste aqui é o download, não a trilha.</summary>
    private sealed class AuditoriaNoOp : IAuditoriaRepository
    {
        public Task RegistrarAsync(RegistroAuditoriaEntity registro, CancellationToken _) => Task.CompletedTask;
    }

    /// <summary>Armazenamento em memória: registra o que foi removido, sem tocar em MinIO.</summary>
    private sealed class ArmazenamentoFalso : IArmazenamentoArquivos
    {
        public List<string> Removidos { get; } = [];
        private readonly Dictionary<string, byte[]> _arquivos = new();

        public Task GravarAsync(string chave, byte[] conteudo, string contentType, CancellationToken _)
        {
            _arquivos[chave] = conteudo;
            return Task.CompletedTask;
        }

        public Task<byte[]?> BaixarAsync(string chave, CancellationToken _) =>
            Task.FromResult(_arquivos.GetValueOrDefault(chave));

        public Task RemoverAsync(string chave, CancellationToken _)
        {
            Removidos.Add(chave);
            _arquivos.Remove(chave);
            return Task.CompletedTask;
        }
    }

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

    private async Task<Guid> CriarSolicitacaoConcluidaAsync(
        ContextoFixo contexto, Conta dono, DateTimeOffset expiraEm)
    {
        Ativar(contexto, dono, somenteLeitura: false);
        var repo = new ExportacaoSolicitacaoWriteRepository(ambiente.CriarGerenciador(contexto));

        var solicitacao = new ExportacaoSolicitacaoEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = dono.TenantId,
            UsuarioSolicitanteId = dono.UsuarioId,
            Formato = FormatoExportacao.Csv,
            Situacao = SituacaoExportacao.Solicitada,
            CriadoEm = DateTimeOffset.UtcNow,
        };

        await repo.CriarAsync(solicitacao, Ct);

        solicitacao.Situacao = SituacaoExportacao.Concluida;
        solicitacao.ReferenciaArquivo = $"exportacoes/{dono.TenantId:N}/{solicitacao.Id:N}.csv";
        solicitacao.NomeArquivo = "homefinance.csv";
        solicitacao.ConcluidoEm = DateTimeOffset.UtcNow;
        solicitacao.ExpiraEm = expiraEm;
        await repo.AtualizarAsync(solicitacao, Ct);

        return solicitacao.Id;
    }

    // ------------------------------------------------------------ 12.14

    /// <summary>
    /// O usuário final não habilita a exportação: o app não tem grant de escrita na tabela, e
    /// uma tentativa direta é recusada pelo banco. A habilitação só existe pela função definer,
    /// que o endpoint administrativo aciona.
    /// </summary>
    [Fact]
    public async Task Escrita_direta_na_habilitacao_e_recusada_pelo_banco()
    {
        var contexto = new ContextoFixo();
        var conta = await CriarContaAsync(contexto);
        Ativar(contexto, conta, somenteLeitura: false);

        await using var escopo = await ambiente.CriarGerenciador(contexto).AbrirEscritaAsync(Ct);
        await using var comando = escopo.Conexao.CreateCommand();
        comando.Transaction = escopo.Transacao;
        comando.CommandText =
            "INSERT INTO exportacao_habilitacao (id, usuario_id, habilitada, motivo, autor_id, criado_em) " +
            "VALUES (@id, @usuario, true, 'tentativa direta', @autor, now())";
        Parametro(comando, "id", Guid.CreateVersion7());
        Parametro(comando, "usuario", conta.UsuarioId);
        Parametro(comando, "autor", conta.UsuarioId);

        var excecao = await Assert.ThrowsAsync<PostgresException>(
            () => comando.ExecuteNonQueryAsync(Ct));

        // 42501 = insufficient_privilege: o role da aplicação não tem INSERT nesta tabela.
        Assert.Equal("42501", excecao.SqlState);
    }

    /// <summary>Pela função definer, porém, a habilitação é gravada — o caminho sancionado funciona.</summary>
    [Fact]
    public async Task Habilitacao_pela_funcao_definer_e_gravada_e_lida_pelo_titular()
    {
        var contexto = new ContextoFixo();
        var conta = await CriarContaAsync(contexto);

        // Escrita: parte da operação, sem tenant selecionado.
        contexto.UsuarioId = Guid.CreateVersion7();
        contexto.TenantId = null;
        contexto.SomenteLeitura = false;
        await new ExportacaoHabilitacaoWriteRepository(ambiente.CriarGerenciador(contexto)).DefinirAsync(
            new ExportacaoHabilitacaoEntity
            {
                Id = Guid.CreateVersion7(),
                UsuarioId = conta.UsuarioId,
                Habilitada = true,
                Motivo = "Requisição de acesso",
                AutorId = contexto.UsuarioId.Value,
                CriadoEm = DateTimeOffset.UtcNow,
            },
            Ct);

        // Leitura: o próprio titular vê a habilitação.
        Ativar(contexto, conta, somenteLeitura: false);
        var habilitada = await new ExportacaoHabilitacaoReadRepository(ambiente.CriarGerenciador(contexto))
            .HabilitadaAsync(conta.UsuarioId, Ct);

        Assert.True(habilitada);
    }

    // ------------------------------------------------------------ 12.15

    /// <summary>
    /// O arquivo expirado é removido do armazenamento e o download passa a ser recusado.
    /// </summary>
    [Fact]
    public async Task Expiracao_remove_arquivo_e_fecha_o_download()
    {
        var contexto = new ContextoFixo();
        var conta = await CriarContaAsync(contexto);

        var solicitacaoId = await CriarSolicitacaoConcluidaAsync(
            contexto, conta, expiraEm: DateTimeOffset.UtcNow.AddHours(-1));

        var armazenamento = new ArmazenamentoFalso();

        // A rotina roda sem tenant selecionado — como em produção.
        var contextoRotina = new ContextoFixo();
        var rotina = new RotinaDeExpiracao(
            new ExportacaoSolicitacaoWriteRepository(ambiente.CriarGerenciador(contextoRotina)),
            armazenamento,
            TimeProvider.System,
            NullLogger<RotinaDeExpiracao>.Instance);

        var resultado = await rotina.ExecutarAsync(Ct);

        Assert.True(resultado.Removidos >= 1);
        Assert.Contains(armazenamento.Removidos, chave => chave.Contains(solicitacaoId.ToString("N")));

        // Situação virou expirada e a referência sumiu.
        Ativar(contexto, conta, somenteLeitura: false);
        var solicitacao = await new ExportacaoSolicitacaoReadRepository(ambiente.CriarGerenciador(contexto))
            .ObterAsync(solicitacaoId, Ct);

        Assert.NotNull(solicitacao);
        Assert.Equal(SituacaoExportacao.Expirada, solicitacao!.Situacao);
        Assert.Null(solicitacao.ReferenciaArquivo);

        // Download agora é recusado.
        var download = await BaixarHandler(contexto).HandleAsync(
            new BaixarExportacaoQuery(conta.TenantId, conta.UsuarioId, solicitacaoId), Ct);

        Assert.False(download.Sucesso);
        Assert.Equal(BaixarExportacaoQueryHandler.CodigoIndisponivel, download.Erros[0].Codigo);
    }

    /// <summary>Uma conta diferente da que solicitou não baixa o arquivo, mesmo dentro do tenant.</summary>
    [Fact]
    public async Task Download_por_outra_conta_e_recusado()
    {
        var contexto = new ContextoFixo();
        var dono = await CriarContaAsync(contexto);

        var solicitacaoId = await CriarSolicitacaoConcluidaAsync(
            contexto, dono, expiraEm: DateTimeOffset.UtcNow.AddDays(1));

        // Outra conta, olhando o tenant do dono (como um destinatário de compartilhamento faria).
        var outra = Guid.CreateVersion7();
        contexto.UsuarioId = outra;
        contexto.TenantId = dono.TenantId;
        contexto.SomenteLeitura = true;

        var download = await BaixarHandler(contexto).HandleAsync(
            new BaixarExportacaoQuery(dono.TenantId, outra, solicitacaoId), Ct);

        Assert.False(download.Sucesso);
        Assert.Equal(BaixarExportacaoQueryHandler.CodigoDeOutraConta, download.Erros[0].Codigo);
    }

    private BaixarExportacaoQueryHandler BaixarHandler(ContextoFixo contexto) =>
        new(new ExportacaoSolicitacaoReadRepository(ambiente.CriarGerenciador(contexto)),
            new ArmazenamentoFalso(),
            new AuditoriaNoOp(),
            TimeProvider.System);

    private static void Parametro(System.Data.Common.DbCommand comando, string nome, object valor)
    {
        var parametro = comando.CreateParameter();
        parametro.ParameterName = nome;
        parametro.Value = valor;
        comando.Parameters.Add(parametro);
    }
}
