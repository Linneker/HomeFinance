namespace Acme.Sistemas.Domain.Enums;

/// <summary>
/// Por onde a assinatura foi contratada.
/// </summary>
/// <remarks>
/// Guardado na assinatura porque cada canal tem seu próprio jeito de confirmar, notificar e
/// cancelar. Sem o canal registrado, a reconciliação não saberia a quem perguntar sobre uma
/// assinatura, e a notificação de um provedor poderia atualizar uma assinatura de outro.
/// </remarks>
public enum CanalPagamento
{
    /// <summary>Compra in-app na App Store. Canal principal no iOS.</summary>
    AppStore = 1,

    /// <summary>Compra in-app no Google Play. Canal principal no Android.</summary>
    GooglePlay = 2,

    MercadoPago = 3,

    Asaas = 4,
}
