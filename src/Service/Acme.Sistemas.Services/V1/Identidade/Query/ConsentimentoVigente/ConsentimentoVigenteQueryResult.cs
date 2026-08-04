using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Identidade.Query.ConsentimentoVigente;

/// <param name="Pendentes">
/// Documentos cuja versão atual ainda não foi aceita. Enquanto houver algum, nenhuma operação
/// sobre dado financeiro é liberada.
/// </param>
public sealed record ConsentimentoVigenteQueryResult(
    bool Vigente,
    IReadOnlyList<string> Pendentes)
{
    public IReadOnlyList<Error> Erros { get; init; } = [];
}
