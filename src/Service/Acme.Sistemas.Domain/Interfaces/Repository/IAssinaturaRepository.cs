using Acme.Sistemas.Domain.Entities.Assinaturas;
using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>Catálogo de planos. É configuração do produto, igual para todo mundo.</summary>
public interface IPlanoReadRepository
{
    Task<IReadOnlyList<PlanoEntity>> ListarAtivosAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Todos os planos, inclusive os inativos. É a visão da operação: a listagem do cliente só
    /// mostra o que está à venda, mas o back-office precisa ver o desativado para reativá-lo.
    /// </summary>
    Task<IReadOnlyList<PlanoEntity>> ListarTodosAsync(CancellationToken cancellationToken);

    Task<PlanoEntity?> ObterPorCodigoAsync(string codigo, CancellationToken cancellationToken);

    Task<PlanoEntity?> ObterPorIdAsync(Guid planoId, CancellationToken cancellationToken);
}

/// <summary>
/// Manutenção do catálogo pela operação.
/// </summary>
/// <remarks>
/// O catálogo tem INSERT/UPDATE revogados do role da aplicação — a semeadura é por migration.
/// A gravação do back-office passa por função <c>SECURITY DEFINER</c>, e só o endpoint sob a
/// política administrativa a alcança.
/// </remarks>
public interface IPlanoWriteRepository
{
    /// <summary>Cria o plano ou atualiza o existente de mesmo código (nome, cota, preço, situação).</summary>
    Task SalvarAsync(PlanoEntity plano, CancellationToken cancellationToken);
}

/// <summary>Leitura da assinatura para a tela de situação.</summary>
public interface IAssinaturaReadRepository
{
    Task<AssinaturaEntity?> VigenteDoUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken);
}

/// <summary>
/// Escrita da assinatura e as leituras que decidem acesso, todas pelo primário.
/// </summary>
/// <remarks>
/// A situação da assinatura é consultada a cada requisição que toca recurso derivado. Ler de
/// réplica atrasada manteria a conta suspensa depois do pagamento confirmado — e é justamente
/// nesse minuto que a pessoa está olhando a tela para ver se voltou.
/// </remarks>
public interface IAssinaturaWriteRepository
{
    Task<AssinaturaEntity?> VigenteDoUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Localiza a assinatura pela referência do provedor, sem contexto de usuário.
    /// </summary>
    /// <remarks>
    /// A notificação do provedor chega sem ninguém autenticado — é o servidor dele falando com o
    /// nosso. Passa por função <c>SECURITY DEFINER</c>, do tamanho exato do problema.
    /// </remarks>
    Task<AssinaturaEntity?> PorReferenciaAsync(
        CanalPagamento canal,
        string referencia,
        CancellationToken cancellationToken);

    Task CriarAsync(AssinaturaEntity assinatura, CancellationToken cancellationToken);

    Task<bool> AtualizarSituacaoAsync(AssinaturaEntity assinatura, CancellationToken cancellationToken);

    /// <summary>Quantos compartilhamentos o tenant do usuário mantém vigentes.</summary>
    Task<int> ContarCompartilhamentosVigentesAsync(CancellationToken cancellationToken);

    Task RegistrarTransacaoAsync(
        TransacaoPagamentoEntity transacao,
        CancellationToken cancellationToken);

    /// <summary>
    /// Registra a notificação recebida. Devolve falso quando já havia sido registrada.
    /// </summary>
    /// <remarks>
    /// É a idempotência: o provedor reenvia até receber confirmação, e processar duas vezes um
    /// estorno suspenderia uma assinatura que já tinha sido regularizada no meio tempo.
    /// </remarks>
    Task<bool> RegistrarNotificacaoAsync(
        CanalPagamento canal,
        string identificador,
        string corpo,
        DateTimeOffset recebidaEm,
        CancellationToken cancellationToken);

    /// <summary>
    /// Situação da assinatura do dono do tenant, sem o contexto dele.
    /// </summary>
    /// <remarks>
    /// Nulo quando o dono não tem assinatura viva. Passa por função <c>SECURITY DEFINER</c>: o
    /// dono é outra pessoa, e a policy de assinatura o esconde de propósito.
    /// </remarks>
    Task<AssinaturaEntity?> DoDonoDoTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Assinaturas a reconciliar com os provedores.</summary>
    Task<IReadOnlyList<AssinaturaEntity>> VigentesParaReconciliacaoAsync(
        CancellationToken cancellationToken);
}
