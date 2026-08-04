using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Identidade.Query.ConsentimentoVigente;

public sealed record ConsentimentoVigenteQuery(
    Guid UsuarioId,
    string VersaoAtual) : IRequest<ConsentimentoVigenteQueryResult>;
