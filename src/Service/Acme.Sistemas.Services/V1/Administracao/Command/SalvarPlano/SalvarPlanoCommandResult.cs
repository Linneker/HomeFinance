using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Administracao.Command.SalvarPlano;

public sealed record SalvarPlanoCommandResult : IResultadoRecusavel<SalvarPlanoCommandResult>
{
    public bool Sucesso { get; init; }

    public Guid? PlanoId { get; init; }

    /// <summary>Verdadeiro quando o plano não existia e foi criado; falso quando um existente foi atualizado.</summary>
    public bool Criado { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static SalvarPlanoCommandResult Salvo(Guid planoId, bool criado) => new()
    {
        Sucesso = true,
        PlanoId = planoId,
        Criado = criado,
    };

    public static SalvarPlanoCommandResult Recusado(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };

    static SalvarPlanoCommandResult IResultadoRecusavel<SalvarPlanoCommandResult>.Recusar(Error erro) => Recusado(erro);
}
