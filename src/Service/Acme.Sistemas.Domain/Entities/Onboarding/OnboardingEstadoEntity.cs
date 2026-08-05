namespace Acme.Sistemas.Domain.Entities.Onboarding;

/// <summary>
/// O que um tenant já cumpriu do onboarding.
/// </summary>
/// <remarks>
/// A linha existe por requisito cumprido — a ausência é o "ainda não". Guardar também os não
/// cumpridos exigiria criar linhas para todo tenant a cada requisito novo, e um requisito
/// acrescentado depois deixaria contas antigas sem estado nenhum.
/// </remarks>
public sealed class OnboardingEstadoEntity : TenantEntityBase
{
    public required string RequisitoCodigo { get; set; }

    public DateTimeOffset? CumpridoEm { get; set; }

    public bool Cumprido => CumpridoEm is not null;
}
