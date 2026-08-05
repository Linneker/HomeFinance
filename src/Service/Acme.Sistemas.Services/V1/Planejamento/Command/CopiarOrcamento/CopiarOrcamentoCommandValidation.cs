using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Entities.Lancamentos;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.CopiarOrcamento;

public static class CopiarOrcamentoCommandValidation
{
    public const string CodigoMesesIguais = "origem_igual_ao_destino";

    public static IReadOnlyList<Error> Validar(CopiarOrcamentoCommand command)
    {
        var origem = CalendarioFinanceiro.PrimeiroDiaDoMes(command.Origem);
        var destino = CalendarioFinanceiro.PrimeiroDiaDoMes(command.Destino);

        // Copiar o mês para ele mesmo apagaria o mês e o reinseriria idêntico. Não corrompe
        // nada, mas é sempre engano de quem chamou — e sem esta recusa o engano passaria
        // silencioso, devolvendo sucesso.
        return origem == destino
            ? [new Error(
                CodigoMesesIguais,
                "Escolha um mês de destino diferente do de origem.",
                nameof(command.Destino))]
            : [];
    }
}
