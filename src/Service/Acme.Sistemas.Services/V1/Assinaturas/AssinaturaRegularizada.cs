using Acme.Sistemas.Core.Mediators.Notification;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Services.V1.Alertas;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Assinaturas;

/// <summary>
/// A assinatura de uma conta voltou a vigorar depois de não estar vigente.
/// </summary>
/// <remarks>
/// Publicado só na virada, e não a cada confirmação de pagamento: quem já estava ativo e renovou
/// não teve recurso nenhum restabelecido, e disparar o efeito de regularização ali faria a fila
/// retida ser varrida todo mês sem ter o que varrer.
/// <para>
/// É evento, e não chamada direta, por uma razão de construção: quem reage à regularização
/// precisa saber a situação da assinatura, e a situação vem do próprio serviço que publica. A
/// chamada direta fecharia o ciclo no contêiner de DI; o evento resolve o ouvinte na publicação,
/// quando o serviço já existe.
/// </para>
/// </remarks>
/// <param name="UsuarioId">Titular cuja assinatura voltou a vigorar.</param>
public sealed record AssinaturaRegularizada(Guid UsuarioId, Guid AssinaturaId) : INotification;

/// <summary>
/// Libera os documentos retidos durante a suspensão assim que a assinatura regulariza.
/// </summary>
/// <remarks>
/// "Sem exigir ação adicional do usuário além do pagamento" tem consequência técnica: o efeito
/// acontece no instante em que o pagamento é confirmado, não no próximo ciclo de uma rotina.
/// <para>
/// O contexto de isolamento é assumido explicitamente porque a regularização chega por dois
/// caminhos e nenhum deles garante o contexto certo: a notificação do provedor não tem token
/// nenhum, e o pedido de contratação pode estar com um tenant de terceiro selecionado. Assumir o
/// tenant do próprio titular é o que faz a fila liberada ser a dele nos dois casos.
/// </para>
/// </remarks>
public sealed class LiberarFilaRetidaAoRegularizar(
    IRotinaReadRepository tenants,
    IContextoAssumivel contexto,
    FilaDeDocumentosRetidos fila,
    ILogger<LiberarFilaRetidaAoRegularizar> logger)
    : INotificationHandler<AssinaturaRegularizada>
{
    public async Task HandleAsync(
        AssinaturaRegularizada notification,
        CancellationToken cancellationToken)
    {
        var tenantId = await tenants.TenantDoUsuarioAsync(notification.UsuarioId, cancellationToken);

        if (tenantId is null)
        {
            // Conta sem tenant próprio — excluída, ou ainda a meio caminho da criação. Não há
            // fila a liberar, e tratar isso como falha faria a contratação devolver erro depois
            // de já ter cobrado.
            logger.LogInformation(
                "Regularização sem tenant próprio para o usuário {Usuario}.", notification.UsuarioId);

            return;
        }

        contexto.Assumir(notification.UsuarioId, tenantId.Value);

        var resultado = await fila.LiberarAsync(cancellationToken);

        logger.LogInformation(
            "Regularização da assinatura {Assinatura}: {Liberados} documento(s) liberado(s).",
            notification.AssinaturaId,
            resultado.Liberados);
    }
}
