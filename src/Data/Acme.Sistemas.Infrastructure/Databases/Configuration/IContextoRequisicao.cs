namespace Acme.Sistemas.Infrastructure.Databases.Configuration;

/// <summary>
/// Contexto de isolamento da requisição, aplicado em toda transação e lido pelas policies de RLS.
/// </summary>
public interface IContextoRequisicao
{
    /// <summary>Usuário autenticado. Nulo antes da autenticação.</summary>
    Guid? UsuarioId { get; }

    /// <summary>
    /// Tenant selecionado — o próprio ou um recebido por compartilhamento. Nulo quando não há
    /// seleção (administrador, criação de conta), e nulo faz toda consulta de dado financeiro
    /// voltar vazia.
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Verdadeiro quando o tenant selecionado não pertence ao usuário. Default seguro é
    /// verdadeiro: sem contexto explícito, nada é gravado.
    /// </summary>
    bool SomenteLeitura { get; }
}
