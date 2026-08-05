using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;

namespace Acme.Sistemas.Services.V1.Assinaturas;

/// <inheritdoc cref="IAssinaturaService"/>
public sealed class AssinaturaService(
    IAssinaturaWriteRepository assinaturas,
    IPlanoReadRepository planos,
    IContextoRequisicao contexto,
    TimeProvider relogio)
    : IAssinaturaService
{
    /// <summary>
    /// Tolerância depois do vencimento antes de suspender.
    /// </summary>
    /// <remarks>
    /// Sete dias cobrem o cartão que falhou numa tentativa e passou na seguinte, que é o caso
    /// comum. Suspender no dia seguinte ao vencimento tiraria os recursos de quem não deixou de
    /// pagar — e recuperar cliente irritado custa mais do que uma semana de tolerância.
    /// </remarks>
    public static readonly TimeSpan Carencia = TimeSpan.FromDays(7);

    public async Task<SituacaoDaConta> SituacaoAtualAsync(CancellationToken cancellationToken)
    {
        if (contexto.UsuarioId is null)
        {
            return SituacaoDaConta.SemAssinatura;
        }

        var assinatura = await assinaturas.VigenteDoUsuarioAsync(
            contexto.UsuarioId.Value, cancellationToken);

        if (assinatura is null)
        {
            return SituacaoDaConta.SemAssinatura;
        }

        var situacao = assinatura.SituacaoEm(relogio.GetUtcNow(), Carencia);

        var plano = await planos.ObterPorIdAsync(assinatura.PlanoId, cancellationToken);

        // A cota usada só importa para o dono decidir se pode conceder mais um. Em tenant de
        // terceiro a pergunta nem faz sentido, e a consulta contaria os compartilhamentos de
        // quem não é ele.
        var usada = contexto.TenantId is not null && !contexto.SomenteLeitura
            ? await assinaturas.ContarCompartilhamentosVigentesAsync(cancellationToken)
            : 0;

        return new SituacaoDaConta(
            situacao, plano?.Codigo ?? string.Empty, plano?.CotaCompartilhamentos ?? 0, usada);
    }

    public async Task<bool> DonoDoTenantVigenteAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var assinatura = await assinaturas.DoDonoDoTenantAsync(tenantId, cancellationToken);

        return assinatura is not null
               && assinatura.SituacaoEm(relogio.GetUtcNow(), Carencia)
                   is SituacaoAssinatura.Ativa or SituacaoAssinatura.Carencia;
    }
}
