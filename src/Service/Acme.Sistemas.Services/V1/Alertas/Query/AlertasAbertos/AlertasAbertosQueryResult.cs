using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Services.V1.Alertas.Query.AlertasAbertos;

/// <param name="Tipo">
/// A natureza vai junto de cada alerta: é o que permite à tela separar "passei do plano" de
/// "vai faltar dinheiro no dia 10", que exigem ações diferentes.
/// </param>
public sealed record AlertaListado(
    Guid Id,
    TipoAlerta Tipo,
    string Mensagem,
    DateTimeOffset CriadoEm);

public sealed record AlertasAbertosQueryResult(IReadOnlyList<AlertaListado> Alertas)
    : IResultadoRecusavel<AlertasAbertosQueryResult>
{
    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static readonly AlertasAbertosQueryResult Vazio = new([]);

    public static AlertasAbertosQueryResult Recusar(Error erro) => new([]) { Erros = [erro] };
}
