using Acme.Sistemas.Domain.Entities.Onboarding;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;

namespace Acme.Sistemas.Services.V1.Onboarding;

/// <inheritdoc cref="IOnboardingService"/>
/// <remarks>
/// Usa o repositório de escrita para as duas leituras: as duas decidem escrita. O progresso é
/// consultado pelo behavior a cada gravação de dado financeiro, e uma réplica atrasada recusaria
/// o primeiro lançamento de quem acabou de declarar a renda — exatamente o momento em que a
/// pessoa está esperando o produto destravar.
/// </remarks>
public sealed class OnboardingService(
    IOnboardingWriteRepository onboarding,
    IContextoRequisicao contexto,
    TimeProvider relogio)
    : IOnboardingService
{
    public async Task<ProgressoDoOnboarding> ProgressoAsync(CancellationToken cancellationToken)
    {
        var catalogo = await onboarding.CatalogoAsync(cancellationToken);

        // Sem tenant selecionado não há estado a cruzar. O progresso vira "nada cumprido", que é
        // o que a tela de quem ainda não escolheu espaço deve mostrar.
        var estados = contexto.TenantId is null
            ? []
            : await onboarding.EstadoDoTenantAsync(cancellationToken);

        return ProgressoDoOnboarding.Montar(catalogo, estados);
    }

    public async Task CumprirAsync(string requisitoCodigo, CancellationToken cancellationToken)
    {
        // Somente leitura é tenant de terceiro: o onboarding é de quem é dono do espaço, e
        // gravar aqui registraria no espaço alheio um passo que quem está olhando não deu.
        if (contexto.TenantId is null || contexto.SomenteLeitura)
        {
            return;
        }

        await onboarding.MarcarCumpridoAsync(
            new OnboardingEstadoEntity
            {
                Id = Guid.CreateVersion7(),
                TenantId = contexto.TenantId.Value,
                RequisitoCodigo = requisitoCodigo,
                CumpridoEm = relogio.GetUtcNow(),
            },
            cancellationToken);
    }
}
