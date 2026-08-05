namespace Acme.Sistemas.Domain.Entities.Assinaturas;

/// <summary>
/// Plano do catálogo, com a cota de compartilhamentos que concede.
/// </summary>
/// <remarks>
/// É configuração do produto, não dado de cliente: a administração muda preço e cota sem
/// alteração de código. Por isso a cota vive aqui e não numa constante — mudá-la para todos os
/// assinantes de um plano tem de ser uma linha no banco, não um deploy.
/// </remarks>
public sealed class PlanoEntity : BaseEntity
{
    /// <summary>Chave estável usada pelas lojas e pelos canais secundários.</summary>
    public required string Codigo { get; set; }

    public required string Nome { get; set; }

    /// <summary>Quantos compartilhamentos simultâneos a conta pode conceder. Zero é válido.</summary>
    public int CotaCompartilhamentos { get; set; }

    public decimal PrecoMensal { get; set; }

    /// <summary>Falso tira o plano da vitrine sem afetar quem já assina.</summary>
    public bool Ativo { get; set; }
}
