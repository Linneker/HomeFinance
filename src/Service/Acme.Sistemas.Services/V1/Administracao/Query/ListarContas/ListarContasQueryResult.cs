using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Services.V1.Administracao.Query.ListarContas;

/// <summary>Uma conta na relação administrativa. Só identificação e situação.</summary>
public sealed record ContaResumo(
    Guid UsuarioId,
    string Email,
    DateTimeOffset CriadoEm,
    SituacaoAssinatura? SituacaoAssinatura,
    string? PlanoCodigo,
    string? PlanoNome);

public sealed record ListarContasQueryResult(IReadOnlyList<ContaResumo> Contas)
{
    public bool Sucesso => true;
}
