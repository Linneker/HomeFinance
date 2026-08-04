using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.RemoverLancamento;

public static class RemoverLancamentoCommandValidation
{
    public const string CodigoLancamentoObrigatorio = "lancamento_obrigatorio";

    public static IReadOnlyList<Error> Validar(RemoverLancamentoCommand command) =>
        command.LancamentoId == Guid.Empty
            ? [new Error(
                CodigoLancamentoObrigatorio,
                "Lançamento não identificado.",
                nameof(command.LancamentoId))]
            : [];
}
