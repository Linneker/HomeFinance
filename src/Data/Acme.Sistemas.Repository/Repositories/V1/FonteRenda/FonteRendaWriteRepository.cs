using Acme.Sistemas.Domain.Entities.Planejamento;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.FonteRenda.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.FonteRenda;

/// <summary>
/// Escrita das fontes de renda — e as leituras que decidem essa escrita, pelo primário.
/// </summary>
public sealed class FonteRendaWriteRepository(TransactionManager transacoes)
    : IFonteRendaWriteRepository
{
    public async Task<IReadOnlyList<FonteRendaEntity>> ListarAsync(
        DateOnly? competencia,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(FonteRendaQuery.Listar(competencia is not null));

        if (competencia is not null)
        {
            comando.Com("competencia", competencia.Value);
        }

        return await FonteRendaReadRepository.LerTodasAsync(comando, cancellationToken);
    }

    public async Task<FonteRendaEntity?> ObterAsync(Guid fonteId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(FonteRendaQuery.Obter)
            .Com("id", fonteId);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        return await leitor.ReadAsync(cancellationToken)
            ? FonteRendaReadRepository.Ler(leitor)
            : null;
    }

    public async Task GravarAsync(FonteRendaEntity fonte, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = Inserir(new EscopoSql(escopo.Conexao, escopo.Transacao), fonte);

        await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task SucederAsync(
        FonteRendaEntity encerrada,
        FonteRendaEntity nova,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        var sql = new EscopoSql(escopo.Conexao, escopo.Transacao);

        await using (var fechamento = sql
            .Comando(FonteRendaCommand.Encerrar)
            .Com("id", encerrada.Id)
            .Com("vigenciaFim", encerrada.VigenciaFim))
        {
            await fechamento.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var insercao = Inserir(sql, nova))
        {
            await insercao.ExecuteNonQueryAsync(cancellationToken);
        }

        await escopo.ConfirmarAsync(cancellationToken);
    }

    public async Task<bool> AlterarAsync(FonteRendaEntity fonte, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(FonteRendaCommand.Alterar)
            .Com("id", fonte.Id)
            .Com("nome", fonte.Nome)
            .Com("valor", fonte.Valor)
            .Com("diaRecebimento", (short)fonte.DiaRecebimento);

        var afetadas = await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);

        return afetadas > 0;
    }

    public async Task<bool> RemoverAsync(Guid fonteId, CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirEscritaAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(FonteRendaCommand.Remover)
            .Com("id", fonteId);

        var afetadas = await comando.ExecuteNonQueryAsync(cancellationToken);
        await escopo.ConfirmarAsync(cancellationToken);

        return afetadas > 0;
    }

    private static System.Data.Common.DbCommand Inserir(EscopoSql sql, FonteRendaEntity fonte) =>
        sql.Comando(FonteRendaCommand.Gravar)
            .Com("id", fonte.Id)
            .Com("tenantId", fonte.TenantId)
            .Com("nome", fonte.Nome)
            .Com("valor", fonte.Valor)
            .Com("diaRecebimento", (short)fonte.DiaRecebimento)
            .Com("vigenciaInicio", fonte.VigenciaInicio)
            .Com("vigenciaFim", fonte.VigenciaFim)
            .Com("criadoEm", fonte.CriadoEm.UtcDateTime);
}
