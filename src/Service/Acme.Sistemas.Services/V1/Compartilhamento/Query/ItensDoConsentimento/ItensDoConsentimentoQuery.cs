using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Query.ItensDoConsentimento;

public sealed record ItensDoConsentimentoQuery(Guid UsuarioId) : IRequest<ItensDoConsentimentoQueryResult>;
