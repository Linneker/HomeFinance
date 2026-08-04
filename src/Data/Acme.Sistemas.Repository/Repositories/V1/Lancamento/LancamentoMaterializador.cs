using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Repository.Helper;

namespace Acme.Sistemas.Repository.Repositories.V1.Lancamento;

/// <summary>
/// Materialização compartilhada pelos repositórios de leitura e de escrita.
/// </summary>
/// <remarks>
/// O repositório de escrita também lê — para decidir a alteração — e as duas leituras precisam
/// produzir exatamente a mesma entidade. Duplicar o mapeamento faria as duas divergirem no dia
/// em que uma coluna nova aparecesse em só um dos lados.
/// </remarks>
internal static class LancamentoMaterializador
{
    public static LancamentoEntity Ler(DbDataReader leitor) => new()
    {
        Id = leitor.Identificador("id"),
        TenantId = leitor.Identificador("tenant_id"),
        Descricao = leitor.TextoObrigatorio("descricao"),
        Valor = leitor.GetDecimal(leitor.GetOrdinal("valor")),
        DataCompetencia = leitor.Data("data_competencia"),
        DataCaixa = leitor.Data("data_caixa"),
        CategoriaId = leitor.IdentificadorOpcional("categoria_id"),
        OrigemClassificacao =
            (OrigemClassificacao)leitor.GetInt16(leitor.GetOrdinal("origem_classificacao")),
        GrupoParcelamentoId = leitor.IdentificadorOpcional("grupo_parcelamento_id"),
        ParcelaNumero = leitor.InteiroOpcional("parcela_numero"),
        ParcelaTotal = leitor.InteiroOpcional("parcela_total"),
        RecorrenciaId = leitor.IdentificadorOpcional("recorrencia_id"),
        Previsto = leitor.GetBoolean(leitor.GetOrdinal("previsto")),
        CriadoEm = leitor.DataHora("criado_em"),
        AlteradoEm = leitor.DataHoraOpcional("alterado_em"),
    };

    public static LancamentoParticipacaoEntity LerParticipacao(DbDataReader leitor) => new()
    {
        Id = leitor.Identificador("id"),
        TenantId = leitor.Identificador("tenant_id"),
        LancamentoId = leitor.Identificador("lancamento_id"),
        UsuarioId = leitor.Identificador("usuario_id"),
        Percentual = leitor.GetDecimal(leitor.GetOrdinal("percentual")),
    };

    public static DbCommand ComLancamento(this DbCommand comando, LancamentoEntity lancamento) =>
        comando
            .Com("id", lancamento.Id)
            .Com("tenantId", lancamento.TenantId)
            .Com("descricao", lancamento.Descricao)
            .Com("valor", lancamento.Valor)
            .Com("dataCompetencia", lancamento.DataCompetencia)
            .Com("dataCaixa", lancamento.DataCaixa)
            .Com("categoriaId", lancamento.CategoriaId)
            .Com("origemClassificacao", (short)lancamento.OrigemClassificacao)
            .Com("grupoParcelamentoId", lancamento.GrupoParcelamentoId)
            .Com("parcelaNumero", lancamento.ParcelaNumero)
            .Com("parcelaTotal", lancamento.ParcelaTotal)
            .Com("recorrenciaId", lancamento.RecorrenciaId)
            .Com("previsto", lancamento.Previsto)
            .Com("criadoEm", lancamento.CriadoEm.UtcDateTime);
}
