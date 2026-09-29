namespace Acme.Sistemas.HomeFinance.App.Services;

/// <summary>
/// Entrega de alertas ao dispositivo do dono do tenant (13.18).
/// </summary>
/// <remarks>
/// A entrega real usa o serviço de push da plataforma (APNs no iOS, FCM no Android, WNS no
/// Windows): o app registra o token do dispositivo no servidor, e o servidor empurra o alerta
/// quando o gera. Esta interface é o ponto onde a implementação de plataforma entra; o registro
/// depende de um endpoint de dispositivos e de credenciais de push que não fazem parte do núcleo
/// da Fase 1, então aqui fica a costura e um stub que não faz entrega — o alerta continua visível
/// na central de alertas (pull), que é a garantia de que nada se perde sem o push.
/// </remarks>
public interface INotificacaoDispositivo
{
    /// <summary>Registra o dispositivo para receber alertas do titular autenticado.</summary>
    Task RegistrarAsync(CancellationToken ct = default);

    /// <summary>Encerra o registro no logout, para o aparelho não receber alerta de conta trocada.</summary>
    Task RemoverAsync(CancellationToken ct = default);
}

/// <summary>Stub sem entrega: a central de alertas (pull) cobre a Fase 1 até o push entrar.</summary>
public sealed class NotificacaoDispositivoStub : INotificacaoDispositivo
{
    public Task RegistrarAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task RemoverAsync(CancellationToken ct = default) => Task.CompletedTask;
}
