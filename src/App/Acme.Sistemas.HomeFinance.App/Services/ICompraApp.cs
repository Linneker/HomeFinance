using Acme.Sistemas.HomeFinance.App.Models;

namespace Acme.Sistemas.HomeFinance.App.Services;

/// <summary>Comprovante devolvido pela loja/gateway, para a Api validar.</summary>
public sealed record ComprovanteCompra(CanalPagamento Canal, string Comprovante);

/// <summary>
/// Compra pelo canal escolhido. A implementação real conversa com a loja (StoreKit / Play Billing)
/// ou com o checkout do gateway (Mercado Pago / Asaas); nenhuma delas trafega dado de cartão pela
/// aplicação — o app recebe só o comprovante, e quem libera o recurso é o servidor, depois de
/// validá-lo. Esta interface é o ponto onde a implementação de plataforma entra.
/// </summary>
public interface ICompraApp
{
    Task<ComprovanteCompra?> ComprarAsync(string planoCodigo, CanalPagamento canal, CancellationToken ct = default);
}

/// <summary>
/// Stub de compra para desenvolvimento e build headless.
/// </summary>
/// <remarks>
/// Devolve um comprovante sintético para exercitar o fluxo ponta a ponta contra a Api sem uma
/// conta de loja. A validação server-side continua real: um comprovante inválido é recusado pela
/// Api, e é isso que garante que nenhum recurso é liberado sem pagamento comprovado. A
/// implementação de loja/gateway substitui este registro por plataforma.
/// </remarks>
public sealed class CompraAppSimulada : ICompraApp
{
    public Task<ComprovanteCompra?> ComprarAsync(string planoCodigo, CanalPagamento canal, CancellationToken ct = default) =>
        Task.FromResult<ComprovanteCompra?>(
            new ComprovanteCompra(canal, $"simulado:{canal}:{planoCodigo}:{Guid.NewGuid():N}"));
}
