using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.CriarLancamento;

public sealed record CriarLancamentoCommandResult
{
    public bool Sucesso { get; init; }

    /// <summary>Identificadores gravados. Mais de um em compra parcelada.</summary>
    public IReadOnlyList<Guid> LancamentoIds { get; init; } = [];

    /// <summary>
    /// Nada foi gravado, e não por erro: há um lançamento igual e falta a confirmação de que
    /// não é digitação repetida. Estado distinto de recusa — o cliente reenvia confirmando.
    /// </summary>
    public bool AguardandoConfirmacaoDeDuplicidade { get; init; }

    public int SemelhantesEncontrados { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static CriarLancamentoCommandResult Criado(IReadOnlyList<Guid> ids) =>
        new() { Sucesso = true, LancamentoIds = ids };

    public static CriarLancamentoCommandResult PossivelDuplicidade(int semelhantes) =>
        new() { Sucesso = false, AguardandoConfirmacaoDeDuplicidade = true, SemelhantesEncontrados = semelhantes };

    public static CriarLancamentoCommandResult Recusado(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };
}
