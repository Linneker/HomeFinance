using System.Net;
using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Core.Response;

/// <summary>
/// Envelope padrão de resposta da plataforma. Todo endpoint devolve este formato, com ou sem
/// conteúdo, para que o cliente tenha sempre a mesma forma para tratar sucesso e erro.
/// </summary>
public sealed record ResponseDefault<T>
{
    public required bool Sucesso { get; init; }

    public required HttpStatusCode StatusCode { get; init; }

    public string? Mensagem { get; init; }

    public T? Conteudo { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static ResponseDefault<T> Ok(T conteudo, string? mensagem = null) => new()
    {
        Sucesso = true,
        StatusCode = HttpStatusCode.OK,
        Mensagem = mensagem,
        Conteudo = conteudo,
    };

    public static ResponseDefault<T> Criado(T conteudo, string? mensagem = null) => new()
    {
        Sucesso = true,
        StatusCode = HttpStatusCode.Created,
        Mensagem = mensagem,
        Conteudo = conteudo,
    };

    public static ResponseDefault<T> Falha(
        HttpStatusCode statusCode,
        string mensagem,
        params Error[] erros) => new()
    {
        Sucesso = false,
        StatusCode = statusCode,
        Mensagem = mensagem,
        Erros = erros,
    };
}
