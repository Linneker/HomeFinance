using Acme.Sistemas.Domain.Entities.Exportacao;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>
/// Reúne o conjunto de dados exportável do tenant ativo.
/// </summary>
/// <remarks>
/// Leitura: o trabalhador da fila roda sob o contexto de isolamento do próprio titular, e a RLS
/// garante que só as linhas daquele tenant venham — a exportação nunca alcança dado de tenant
/// recebido por compartilhamento porque a própria consulta não o enxerga.
/// </remarks>
public interface IExportacaoDadosReadRepository
{
    Task<DadosDoTenantParaExportacao> ColetarAsync(CancellationToken cancellationToken);
}
