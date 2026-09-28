using Acme.Sistemas.Domain.Entities.Exportacao;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>Identificadores de uma solicitação expirada — o bastante para apagar o arquivo.</summary>
public sealed record SolicitacaoExpirada(Guid Id, Guid TenantId, string? ReferenciaArquivo);

/// <summary>Leitura de solicitações de exportação — situação e download, sob o tenant ativo.</summary>
public interface IExportacaoSolicitacaoReadRepository
{
    Task<ExportacaoSolicitacaoEntity?> ObterAsync(Guid id, CancellationToken cancellationToken);
}

/// <summary>Escrita de solicitações e as leituras/rotinas que decidem escrita.</summary>
public interface IExportacaoSolicitacaoWriteRepository
{
    Task CriarAsync(ExportacaoSolicitacaoEntity solicitacao, CancellationToken cancellationToken);

    Task<ExportacaoSolicitacaoEntity?> ObterAsync(Guid id, CancellationToken cancellationToken);

    Task AtualizarAsync(ExportacaoSolicitacaoEntity solicitacao, CancellationToken cancellationToken);

    /// <summary>
    /// Solicitações concluídas cujo prazo já passou, de todos os tenants.
    /// </summary>
    /// <remarks>
    /// Enumera por função <c>SECURITY DEFINER</c> estreita — o mesmo recurso da rotina de
    /// alertas —, porque a limpeza roda sem tenant selecionado e a RLS normal não devolveria
    /// linha nenhuma. A função só entrega identificadores e a referência do arquivo; nenhum
    /// dado financeiro passa por ela.
    /// </remarks>
    Task<IReadOnlyList<SolicitacaoExpirada>> ExpiradasAsync(
        DateTimeOffset agora,
        CancellationToken cancellationToken);

    /// <summary>Marca uma solicitação como expirada e esquece a referência do arquivo já removido.</summary>
    Task ExpirarAsync(Guid solicitacaoId, CancellationToken cancellationToken);

    /// <summary>
    /// Cancela as solicitações ainda não concluídas de uma conta — chamado na desabilitação, que
    /// parte do administrador e portanto não tem o tenant do titular selecionado.
    /// </summary>
    Task<int> CancelarPendentesAsync(Guid usuarioId, CancellationToken cancellationToken);
}
