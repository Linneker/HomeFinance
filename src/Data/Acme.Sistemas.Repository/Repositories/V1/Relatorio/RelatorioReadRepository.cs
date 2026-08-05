using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Relatorios;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Relatorio.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Relatorio;

public sealed class RelatorioReadRepository(TransactionManager transacoes) : IRelatorioReadRepository
{
    public async Task<IReadOnlyList<LancamentoApuradoEntity>> ExtratoAsync(
        PeriodoDoRelatorio periodo,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(RelatorioQuery.Extrato(periodo.PorCaixa))
            .Com("de", periodo.De)
            .Com("ate", periodo.Ate);

        var linhas = new List<LancamentoApuradoEntity>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            linhas.Add(Ler(leitor));
        }

        return linhas;
    }

    public async Task<IReadOnlyList<TotalPorCategoria>> TotaisPorCategoriaAsync(
        PeriodoDoRelatorio periodo,
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(RelatorioQuery.TotaisPorCategoria(periodo.PorCaixa))
            .Com("de", periodo.De)
            .Com("ate", periodo.Ate);

        var totais = new List<TotalPorCategoria>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            totais.Add(new TotalPorCategoria(
                leitor.IdentificadorOpcional("categoria_id"),
                leitor.GetDecimal(leitor.GetOrdinal("realizado")),
                (int)leitor.GetInt64(leitor.GetOrdinal("quantidade"))));
        }

        return totais;
    }

    private static LancamentoApuradoEntity Ler(DbDataReader leitor) => new()
    {
        Id = leitor.Identificador("id"),
        TenantId = leitor.Identificador("tenant_id"),
        Descricao = leitor.TextoObrigatorio("descricao"),
        Valor = leitor.GetDecimal(leitor.GetOrdinal("valor")),
        ValorApurado = leitor.GetDecimal(leitor.GetOrdinal("valor_apurado")),
        DataCompetencia = leitor.Data("data_competencia"),
        DataCaixa = leitor.Data("data_caixa"),
        CategoriaId = leitor.IdentificadorOpcional("categoria_id"),
        GrupoParcelamentoId = leitor.IdentificadorOpcional("grupo_parcelamento_id"),
        ParcelaNumero = leitor.InteiroOpcional("parcela_numero"),
        ParcelaTotal = leitor.InteiroOpcional("parcela_total"),
        RecorrenciaId = leitor.IdentificadorOpcional("recorrencia_id"),
        Previsto = leitor.GetBoolean(leitor.GetOrdinal("previsto")),
        CriadoEm = leitor.DataHora("criado_em"),
    };
}
