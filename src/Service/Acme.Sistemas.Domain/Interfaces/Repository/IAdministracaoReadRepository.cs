using Acme.Sistemas.Domain.Entities.Administracao;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>
/// Leitura administrativa de contas — só identificação e situação, sem valor financeiro.
/// </summary>
/// <remarks>
/// A leitura passa por função <c>SECURITY DEFINER</c> que projeta apenas colunas de conta e de
/// situação de assinatura. Não é a conexão de leitura comum: o administrador não seleciona
/// tenant, então a RLS por tenant devolveria vazio nas tabelas financeiras de qualquer forma —
/// e nenhuma função dá a ele acesso a elas.
/// </remarks>
public interface IAdministracaoReadRepository
{
    /// <summary>Relação de contas ativas, ordenada por data de criação. Contas excluídas não entram.</summary>
    Task<IReadOnlyList<ContaAdministrativa>> ListarContasAsync(CancellationToken cancellationToken);
}
