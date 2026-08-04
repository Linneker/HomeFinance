using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Query.ItensDoConsentimento;

public static class ItensDoConsentimentoQueryValidation
{
    public const string CodigoUsuarioObrigatorio = "usuario_obrigatorio";

    public static IReadOnlyList<Error> Validar(ItensDoConsentimentoQuery query) =>
        query.UsuarioId == Guid.Empty
            ? [new Error(CodigoUsuarioObrigatorio, "Titular não identificado.")]
            : [];
}
