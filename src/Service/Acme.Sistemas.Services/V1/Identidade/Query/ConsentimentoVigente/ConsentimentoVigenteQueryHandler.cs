using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Identidade.Query.ConsentimentoVigente;

/// <summary>
/// Verifica se o usuário aceitou a versão vigente de todos os documentos obrigatórios.
/// </summary>
/// <remarks>
/// Query, então usa o repositório de LEITURA — nenhuma decisão de escrita depende disto.
/// </remarks>
public sealed class ConsentimentoVigenteQueryHandler(IUsuarioReadRepository usuarios)
    : IRequestHandler<ConsentimentoVigenteQuery, ConsentimentoVigenteQueryResult>
{
    public async Task<ConsentimentoVigenteQueryResult> HandleAsync(
        ConsentimentoVigenteQuery request,
        CancellationToken cancellationToken)
    {
        var erros = ConsentimentoVigenteQueryValidation.Validar(request);

        if (erros.Count > 0)
        {
            return new ConsentimentoVigenteQueryResult(false, []) { Erros = erros };
        }

        var aceitos = await usuarios.ObterConsentimentosAsync(request.UsuarioId, cancellationToken);

        var pendentes = DocumentoConsentimento.Obrigatorios
            .Where(documento => !aceitos.Any(aceito =>
                aceito.Documento == documento
                && string.Equals(aceito.Versao, request.VersaoAtual, StringComparison.Ordinal)))
            .ToList();

        return new ConsentimentoVigenteQueryResult(pendentes.Count == 0, pendentes);
    }
}
