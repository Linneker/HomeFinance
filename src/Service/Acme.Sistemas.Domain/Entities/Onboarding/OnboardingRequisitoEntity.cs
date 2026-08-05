namespace Acme.Sistemas.Domain.Entities.Onboarding;

/// <summary>
/// Um passo do onboarding, como configuração do produto.
/// </summary>
/// <remarks>
/// <see cref="Declarado"/> e <see cref="Ativo"/> são coisas diferentes, e é essa separação que
/// permite mostrar o caminho inteiro desde o primeiro dia. Um requisito que depende de recurso
/// ainda não entregue aparece na barra como etapa futura sem travar ninguém — sem isso, ou ele
/// some da tela (e a pessoa não sabe onde vai dar) ou bloqueia uma ação que o produto ainda não
/// sabe cumprir.
/// </remarks>
public sealed class OnboardingRequisitoEntity
{
    /// <summary>Chave estável. É o que liga a configuração ao estado do tenant.</summary>
    public required string Codigo { get; set; }

    public required string Descricao { get; set; }

    public int Ordem { get; set; }

    /// <summary>Conta no progresso e pode bloquear. Falso é etapa apenas anunciada.</summary>
    public bool Ativo { get; set; }

    /// <summary>Impede a escrita de dado financeiro enquanto não for cumprido.</summary>
    public bool Bloqueante { get; set; }

    /// <summary>
    /// Bloqueia de fato. Declarado sem estar ativo nunca bloqueia, mesmo marcado como
    /// bloqueante — a marca descreve a intenção, o ativo descreve o que já vale.
    /// </summary>
    public bool BloqueiaAgora => Ativo && Bloqueante;

    /// <summary>Requisito cujo cumprimento é o cadastro da primeira fonte de renda.</summary>
    public const string CodigoRendaDeclarada = "renda_declarada";
}
