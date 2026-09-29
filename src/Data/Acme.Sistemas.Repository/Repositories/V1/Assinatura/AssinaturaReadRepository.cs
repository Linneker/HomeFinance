using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Assinaturas;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Assinatura.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Assinatura;

public sealed class AssinaturaReadRepository(TransactionManager transacoes)
    : IAssinaturaReadRepository, IPlanoReadRepository
{
    public async Task<AssinaturaEntity?> VigenteDoUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AssinaturaQuery.VigenteDoUsuario)
            .Com("usuarioId", usuarioId);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        return await leitor.ReadAsync(cancellationToken) ? Ler(leitor) : null;
    }

    public async Task<IReadOnlyList<PlanoEntity>> ListarAtivosAsync(
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AssinaturaQuery.PlanosAtivos);

        var planos = new List<PlanoEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            planos.Add(LerPlano(leitor));
        }

        return planos;
    }

    public async Task<IReadOnlyList<PlanoEntity>> ListarTodosAsync(
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AssinaturaQuery.PlanosTodos);

        var planos = new List<PlanoEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            planos.Add(LerPlano(leitor));
        }

        return planos;
    }

    public async Task<PlanoEntity?> ObterPorCodigoAsync(
        string codigo,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AssinaturaQuery.PlanoPorCodigo)
            .Com("codigo", codigo);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        return await leitor.ReadAsync(cancellationToken) ? LerPlano(leitor) : null;
    }

    public async Task<PlanoEntity?> ObterPorIdAsync(Guid planoId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AssinaturaQuery.PlanoPorId)
            .Com("id", planoId);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        return await leitor.ReadAsync(cancellationToken) ? LerPlano(leitor) : null;
    }

    internal static PlanoEntity LerPlano(DbDataReader leitor) => new()
    {
        Id = leitor.Identificador("id"),
        Codigo = leitor.TextoObrigatorio("codigo"),
        Nome = leitor.TextoObrigatorio("nome"),
        CotaCompartilhamentos = leitor.GetInt32(leitor.GetOrdinal("cota_compartilhamentos")),
        PrecoMensal = leitor.GetDecimal(leitor.GetOrdinal("preco_mensal")),
        Ativo = leitor.GetBoolean(leitor.GetOrdinal("ativo")),
        CriadoEm = leitor.DataHora("criado_em"),
    };

    internal static AssinaturaEntity Ler(DbDataReader leitor) => new()
    {
        Id = leitor.Identificador("id"),
        UsuarioId = leitor.Identificador("usuario_id"),
        PlanoId = leitor.Identificador("plano_id"),
        Situacao = (SituacaoAssinatura)leitor.GetInt16(leitor.GetOrdinal("situacao")),
        Canal = (CanalPagamento)leitor.GetInt16(leitor.GetOrdinal("canal")),
        PeriodoFim = leitor.DataHora("periodo_fim"),
        CarenciaAte = leitor.DataHoraOpcional("carencia_ate"),
        ComprovanteReferencia = leitor.TextoObrigatorio("comprovante_referencia"),
        CriadoEm = leitor.DataHora("criado_em"),
        AtualizadoEm = leitor.DataHoraOpcional("atualizado_em"),
    };
}
