namespace Acme.Sistemas.Services.V1.Administracao.Query.ListarPlanos;

public sealed record PlanoResumo(
    Guid Id,
    string Codigo,
    string Nome,
    int CotaCompartilhamentos,
    decimal PrecoMensal,
    bool Ativo);

public sealed record ListarPlanosQueryResult(IReadOnlyList<PlanoResumo> Planos)
{
    public bool Sucesso => true;
}
