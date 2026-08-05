using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.DefinirOrcamento;

public static class DefinirOrcamentoCommandValidation
{
    public const string CodigoCategoriaObrigatoria = "categoria_obrigatoria";
    public const string CodigoValorNegativo = "valor_planejado_negativo";

    public static IReadOnlyList<Error> Validar(DefinirOrcamentoCommand command)
    {
        var erros = new List<Error>();

        if (command.CategoriaId == Guid.Empty)
        {
            erros.Add(new Error(
                CodigoCategoriaObrigatoria,
                "Informe a categoria.",
                nameof(command.CategoriaId)));
        }

        // Zero é válido e negativo não: planejar zero é a decisão explícita de não gastar
        // naquela categoria no mês, e é diferente de não ter orçamento nenhum. Valor negativo
        // não tem leitura possível — inverteria o sinal de toda comparação com o gasto real.
        if (command.ValorPlanejado < 0)
        {
            erros.Add(new Error(
                CodigoValorNegativo,
                "O valor planejado não pode ser negativo.",
                nameof(command.ValorPlanejado)));
        }

        return erros;
    }
}
