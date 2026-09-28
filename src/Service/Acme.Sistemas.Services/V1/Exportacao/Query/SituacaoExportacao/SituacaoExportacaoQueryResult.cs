using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Services.V1.Exportacao.Query.SituacaoExportacao;

public sealed record SituacaoExportacaoQueryResult : IResultadoRecusavel<SituacaoExportacaoQueryResult>
{
    public bool Encontrada { get; init; }

    public Acme.Sistemas.Domain.Enums.SituacaoExportacao Situacao { get; init; }

    public FormatoExportacao Formato { get; init; }

    public DateTimeOffset? ExpiraEm { get; init; }

    /// <summary>Concluída, com arquivo, dentro do prazo. É o que a tela usa para liberar o botão.</summary>
    public bool DisponivelParaDownload { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static SituacaoExportacaoQueryResult NaoEncontrada() => new() { Encontrada = false };

    public static SituacaoExportacaoQueryResult De(
        Acme.Sistemas.Domain.Enums.SituacaoExportacao situacao,
        FormatoExportacao formato,
        DateTimeOffset? expiraEm,
        bool disponivel) =>
        new()
        {
            Encontrada = true,
            Situacao = situacao,
            Formato = formato,
            ExpiraEm = expiraEm,
            DisponivelParaDownload = disponivel,
        };

    public static SituacaoExportacaoQueryResult Recusar(Error erro) => new() { Erros = [erro] };
}
