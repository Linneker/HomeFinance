namespace Acme.Sistemas.Domain.Entities.Exportacao;

/// <summary>Um lançamento, achatado para exportação.</summary>
/// <remarks>
/// Forma própria, e não a entidade de domínio: a exportação é um contrato com o titular sobre o
/// que ele leva embora, e amarrá-la à entidade faria toda mudança interna do modelo vazar para o
/// arquivo que alguém abre meses depois. As duas datas vão juntas — competência e caixa —, porque
/// tirar uma delas esconderia metade do sentido de cada lançamento.
/// </remarks>
public sealed record LancamentoExportado(
    DateOnly Competencia,
    DateOnly Caixa,
    string Descricao,
    decimal Valor,
    string Tipo,
    string? Categoria);

public sealed record CategoriaExportada(string Nome, string? Tipo);

public sealed record OrcamentoExportado(DateOnly Competencia, string Categoria, decimal ValorPlanejado);

public sealed record FonteRendaExportada(
    string Descricao,
    decimal Valor,
    DateOnly VigenteDe,
    DateOnly? VigenteAte);

/// <summary>Conjunto completo dos dados de um tenant, pronto para os formatadores.</summary>
public sealed record DadosDoTenantParaExportacao(
    IReadOnlyList<LancamentoExportado> Lancamentos,
    IReadOnlyList<CategoriaExportada> Categorias,
    IReadOnlyList<OrcamentoExportado> Orcamentos,
    IReadOnlyList<FonteRendaExportada> FontesRenda);
