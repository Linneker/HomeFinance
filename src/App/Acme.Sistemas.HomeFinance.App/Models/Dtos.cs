namespace Acme.Sistemas.HomeFinance.App.Models;

// Espelhos dos contratos de resposta/requisição da Api (camelCase no fio). Só o que o app usa.

// ---- Assinatura ----
public sealed record Plano(string Codigo, string Nome, int CotaCompartilhamentos, decimal PrecoMensal);
public sealed record Catalogo(IReadOnlyList<Plano> Planos);
public sealed record SituacaoAssinaturaResposta(
    SituacaoAssinatura Situacao,
    bool Vigente,
    string PlanoCodigo,
    int CotaTotal,
    int CotaUsada,
    int CotaDisponivel);
public sealed record Contratacao(Guid AssinaturaId);

// ---- Tenants ----
public sealed record TenantDisponivel(Guid TenantId, bool EhProprio, bool SomenteLeitura);
public sealed record TenantsDisponiveis(IReadOnlyList<TenantDisponivel> Tenants);

// ---- Categorias ----
public sealed record CategoriaItem(Guid Id, string Nome);
public sealed record CategoriasResposta(IReadOnlyList<CategoriaItem> Categorias);
public sealed record CategoriaCriada(Guid CategoriaId);

// ---- Lançamentos ----
public sealed record LancamentoItem(
    Guid Id,
    string Descricao,
    decimal Valor,
    DateOnly DataCompetencia,
    DateOnly DataCaixa,
    Guid? CategoriaId,
    bool PendenteDeClassificacao,
    string Tipo,
    string? Parcelamento,
    bool Previsto);
public sealed record LancamentosResposta(IReadOnlyList<LancamentoItem> Lancamentos, bool VisaoPorCaixa, decimal Total);
public sealed record LancamentoCriado(IReadOnlyList<Guid> LancamentoIds);
public sealed record PossivelDuplicidade(int Semelhantes, string Mensagem);
public sealed record RecorrenciaCriada(Guid RecorrenciaId, int OcorrenciasGeradas);
public sealed record LancamentoAlterado(IReadOnlyList<DateOnly> CompetenciasAfetadas);
public sealed record ClassificacaoAplicada(DateOnly CompetenciaAfetada, Guid? RegraId, string? PadraoDaRegra);

// ---- Renda ----
public sealed record FonteRendaItem(
    Guid Id,
    string Nome,
    decimal Valor,
    int DiaRecebimento,
    DateOnly VigenciaInicio,
    DateOnly? VigenciaFim,
    DateOnly? Recebimento);
public sealed record FontesRenda(IReadOnlyList<FonteRendaItem> Fontes, decimal RendaPrevista);
public sealed record FonteRendaCadastrada(Guid Id, DateOnly VigenciaInicio, decimal RendaPrevista);

// ---- Orçamento ----
public sealed record OrcamentoItem(Guid Id, Guid CategoriaId, decimal ValorPlanejado);
public sealed record OrcamentoDoMes(
    DateOnly Competencia,
    IReadOnlyList<OrcamentoItem> Categorias,
    decimal TotalPlanejado,
    decimal RendaPrevista,
    string? Aviso);
public sealed record OrcamentoDefinido(Guid Id, DateOnly Competencia, decimal TotalPlanejado, decimal RendaPrevista, string? Aviso);
public sealed record OrcamentoCopiado(DateOnly Destino, int LinhasCopiadas, decimal TotalPlanejado, decimal RendaPrevista, string? Aviso);

// ---- Alertas ----
public sealed record AlertaItem(Guid Id, TipoAlerta Tipo, string Mensagem, DateTimeOffset CriadoEm);
public sealed record AlertasResposta(IReadOnlyList<AlertaItem> Alertas);

// ---- Regras de classificação ----
public sealed record RegraItem(Guid Id, string Padrao, Guid CategoriaId, DateTimeOffset CriadaEm);
public sealed record RegrasResposta(IReadOnlyList<RegraItem> Regras);

// ---- Relatório: resumo por categoria ----
public sealed record LinhaResumo(Guid CategoriaId, decimal Planejado, decimal Realizado, int Quantidade, bool Excedida, decimal Disponivel, decimal Excedente);
public sealed record TotalSemPlano(Guid? CategoriaId, decimal Realizado, int Quantidade);
public sealed record ForaDoPlanejamento(IReadOnlyList<TotalSemPlano> Categorias, decimal Total, decimal PendenteDeClassificacao);
public sealed record Resumo(
    DateOnly Competencia,
    bool VisaoPorCaixa,
    IReadOnlyList<LinhaResumo> Categorias,
    ForaDoPlanejamento ForaDoPlanejamento,
    decimal TotalPlanejado,
    decimal TotalRealizado,
    decimal Diferenca);

// ---- Compartilhamento ----
public sealed record ItemConsentimento(string Chave, string Titulo, string Descricao, bool Sensivel, bool MarcadoPorPadrao);
public sealed record ItensConsentimento(IReadOnlyList<ItemConsentimento> Itens);
public sealed record CompartilhamentoCriado(Guid CompartilhamentoId);
public sealed record CompartilhamentoConcedido(Guid DestinatarioId, bool IncluiScr, DateTimeOffset ConcedidoEm);

// ---- Exportação ----
public sealed record ExportacaoSolicitada(Guid SolicitacaoId);
public sealed record SituacaoExportacaoResposta(
    SituacaoExportacao Situacao,
    FormatoExportacao Formato,
    DateTimeOffset? ExpiraEm,
    bool DisponivelParaDownload);

// ---- Exclusão de conta ----
public sealed record ContaExcluida(bool Excluida);

// ---- Onboarding ----
public sealed record PassoOnboarding(string Codigo, string Descricao, int Ordem, bool Cumprido, bool Bloqueante, bool Futuro);
public sealed record ProgressoOnboarding(
    IReadOnlyList<PassoOnboarding> Passos,
    IReadOnlyList<PassoOnboarding> Pendentes,
    int PercentualConcluido,
    bool Concluido,
    bool Liberado);
