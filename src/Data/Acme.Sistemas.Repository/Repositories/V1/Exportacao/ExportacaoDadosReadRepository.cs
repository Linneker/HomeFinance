using Acme.Sistemas.Domain.Entities.Exportacao;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Exportacao.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Exportacao;

/// <inheritdoc cref="IExportacaoDadosReadRepository"/>
public sealed class ExportacaoDadosReadRepository(TransactionManager transacoes)
    : IExportacaoDadosReadRepository
{
    public async Task<DadosDoTenantParaExportacao> ColetarAsync(CancellationToken cancellationToken)
    {
        // Uma transação de leitura para o conjunto inteiro: as quatro consultas veem o mesmo
        // instante do banco, e o arquivo não sai com lançamentos de um segundo e orçamento de
        // outro.
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);

        var lancamentos = await LerLancamentosAsync(escopo, cancellationToken);
        var categorias = await LerCategoriasAsync(escopo, cancellationToken);
        var orcamentos = await LerOrcamentosAsync(escopo, cancellationToken);
        var fontes = await LerFontesAsync(escopo, cancellationToken);

        return new DadosDoTenantParaExportacao(lancamentos, categorias, orcamentos, fontes);
    }

    private static async Task<IReadOnlyList<LancamentoExportado>> LerLancamentosAsync(
        EscopoTransacao escopo, CancellationToken cancellationToken)
    {
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(ExportacaoDadosQuery.Lancamentos);

        var lista = new List<LancamentoExportado>();
        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            lista.Add(new LancamentoExportado(
                leitor.Data("data_competencia"),
                leitor.Data("data_caixa"),
                leitor.TextoObrigatorio("descricao"),
                leitor.GetDecimal(leitor.GetOrdinal("valor")),
                leitor.TextoObrigatorio("tipo"),
                leitor.IsDBNull(leitor.GetOrdinal("categoria"))
                    ? null
                    : leitor.TextoObrigatorio("categoria")));
        }

        return lista;
    }

    private static async Task<IReadOnlyList<CategoriaExportada>> LerCategoriasAsync(
        EscopoTransacao escopo, CancellationToken cancellationToken)
    {
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(ExportacaoDadosQuery.Categorias);

        var lista = new List<CategoriaExportada>();
        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            lista.Add(new CategoriaExportada(leitor.TextoObrigatorio("nome"), null));
        }

        return lista;
    }

    private static async Task<IReadOnlyList<OrcamentoExportado>> LerOrcamentosAsync(
        EscopoTransacao escopo, CancellationToken cancellationToken)
    {
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(ExportacaoDadosQuery.Orcamentos);

        var lista = new List<OrcamentoExportado>();
        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            lista.Add(new OrcamentoExportado(
                leitor.Data("competencia"),
                leitor.TextoObrigatorio("categoria"),
                leitor.GetDecimal(leitor.GetOrdinal("valor_planejado"))));
        }

        return lista;
    }

    private static async Task<IReadOnlyList<FonteRendaExportada>> LerFontesAsync(
        EscopoTransacao escopo, CancellationToken cancellationToken)
    {
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(ExportacaoDadosQuery.FontesRenda);

        var lista = new List<FonteRendaExportada>();
        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            lista.Add(new FonteRendaExportada(
                leitor.TextoObrigatorio("nome"),
                leitor.GetDecimal(leitor.GetOrdinal("valor")),
                leitor.Data("vigencia_inicio"),
                leitor.DataOpcional("vigencia_fim")));
        }

        return lista;
    }
}
