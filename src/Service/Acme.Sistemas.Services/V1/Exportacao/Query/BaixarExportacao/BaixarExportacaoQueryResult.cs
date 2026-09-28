using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Exportacao.Query.BaixarExportacao;

public sealed record BaixarExportacaoQueryResult : IResultadoRecusavel<BaixarExportacaoQueryResult>
{
    public bool Sucesso { get; init; }

    public byte[]? Conteudo { get; init; }

    public string? ContentType { get; init; }

    public string? NomeArquivo { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static BaixarExportacaoQueryResult Baixado(byte[] conteudo, string contentType, string nome) =>
        new() { Sucesso = true, Conteudo = conteudo, ContentType = contentType, NomeArquivo = nome };

    public static BaixarExportacaoQueryResult Recusado(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };

    static BaixarExportacaoQueryResult IResultadoRecusavel<BaixarExportacaoQueryResult>.Recusar(Error erro) => Recusado(erro);
}
