using Acme.Sistemas.Domain.Entities.Alertas;
using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Domain.Interfaces.Repository;

/// <summary>Leitura dos alertas para a tela.</summary>
public interface IAlertaReadRepository
{
    /// <summary>Alertas visíveis do tenant ativo, do mais recente para o mais antigo.</summary>
    Task<IReadOnlyList<AlertaEntity>> ListarAbertosAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Escrita dos alertas e as leituras que decidem essa escrita, todas pelo primário.
/// </summary>
/// <remarks>
/// <see cref="ListarPorCondicaoAtivaAsync"/> vive aqui porque decide se um alerta será emitido.
/// Consultá-la em réplica atrasada duplicaria o aviso que a pessoa acabou de dispensar.
/// </remarks>
public interface IAlertaWriteRepository
{
    /// <summary>
    /// Alertas cuja condição ainda vale — abertos e dispensados — para o tipo informado.
    /// </summary>
    Task<IReadOnlyList<AlertaEntity>> ListarPorCondicaoAtivaAsync(
        TipoAlerta tipo,
        CancellationToken cancellationToken);

    Task<AlertaEntity?> ObterAsync(Guid alertaId, CancellationToken cancellationToken);

    Task GravarAsync(AlertaEntity alerta, CancellationToken cancellationToken);

    /// <summary>Aplica o novo estado e a data correspondente.</summary>
    Task<bool> AtualizarEstadoAsync(AlertaEntity alerta, CancellationToken cancellationToken);
}

/// <summary>
/// Enumeração dos tenants para a rotina diária.
/// </summary>
/// <remarks>
/// Existe porque a rotina roda sem requisição e, portanto, sem tenant selecionado: sob a RLS
/// normal ela não enxergaria tenant nenhum e nunca reavaliaria nada. A abertura é uma função
/// <c>SECURITY DEFINER</c> que devolve só identificadores — o mesmo padrão já usado no login,
/// e do tamanho exato do problema.
/// </remarks>
public interface IRotinaReadRepository
{
    Task<IReadOnlyList<TenantParaRotina>> TenantsAtivosAsync(CancellationToken cancellationToken);

    /// <summary>
    /// O tenant próprio do usuário informado, sem depender do contexto da requisição.
    /// </summary>
    /// <remarks>
    /// Serve ao trabalho disparado pela regularização da assinatura, que pode chegar pela
    /// notificação do provedor — sem token, sem usuário e sem tenant. Sai da mesma função
    /// <c>SECURITY DEFINER</c> da rotina, e não de uma abertura nova: o problema é o mesmo, e
    /// duas aberturas para a mesma pergunta seriam duas para manter em dia.
    /// </remarks>
    Task<Guid?> TenantDoUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken);
}

/// <param name="UsuarioId">Dono do tenant. A rotina age sempre como o dono, nunca como convidado.</param>
public sealed record TenantParaRotina(Guid TenantId, Guid UsuarioId);
