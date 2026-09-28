using Acme.Sistemas.Domain.Entities.Exportacao;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>
/// Leitura da habilitação de exportação — atende à Query que decide se a tela mostra o recurso e
/// à checagem que libera a solicitação, ambas sob o contexto do próprio titular.
/// </summary>
public interface IExportacaoHabilitacaoReadRepository
{
    /// <summary>Verdadeiro apenas se a conta tem uma habilitação vigente. Ausência é desligado.</summary>
    Task<bool> HabilitadaAsync(Guid usuarioId, CancellationToken cancellationToken);
}

/// <summary>
/// Escrita da habilitação, exclusiva da administração.
/// </summary>
/// <remarks>
/// A gravação passa por função <c>SECURITY DEFINER</c>, e o app não recebe INSERT/UPDATE diretos
/// na tabela: quem liga a exportação de dados financeiros é a operação, por endpoint autenticado
/// como administrador, e estreitar o caminho no próprio banco é uma trava a mais sobre um recurso
/// que, ligado por engano, exporia o dado mais sensível do sistema.
/// </remarks>
public interface IExportacaoHabilitacaoWriteRepository
{
    /// <summary>Grava a decisão — insere na primeira vez, atualiza nas seguintes.</summary>
    Task DefinirAsync(ExportacaoHabilitacaoEntity habilitacao, CancellationToken cancellationToken);

    /// <summary>
    /// Checagem que libera a solicitação, pela conexão de escrita: ela decide um Command, e ler
    /// por réplica atrasada geraria uma exportação cuja habilitação já tinha sido revogada.
    /// </summary>
    Task<bool> HabilitadaAsync(Guid usuarioId, CancellationToken cancellationToken);
}
