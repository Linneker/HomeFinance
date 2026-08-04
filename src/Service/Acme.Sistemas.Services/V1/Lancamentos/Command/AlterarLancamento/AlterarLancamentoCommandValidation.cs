using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.AlterarLancamento;

public static class AlterarLancamentoCommandValidation
{
    public const string CodigoLancamentoObrigatorio = "lancamento_obrigatorio";
    public const string CodigoDescricaoObrigatoria = "descricao_obrigatoria";
    public const string CodigoValorInvalido = "valor_invalido";
    public const string CodigoCompetenciaObrigatoria = "data_competencia_obrigatoria";
    public const string CodigoCaixaObrigatoria = "data_caixa_obrigatoria";

    public static IReadOnlyList<Error> Validar(AlterarLancamentoCommand command)
    {
        var erros = new List<Error>();

        if (command.LancamentoId == Guid.Empty)
        {
            erros.Add(new Error(
                CodigoLancamentoObrigatorio, "Lançamento não identificado.", nameof(command.LancamentoId)));
        }

        if (string.IsNullOrWhiteSpace(command.Descricao))
        {
            erros.Add(new Error(
                CodigoDescricaoObrigatoria, "Informe a descrição.", nameof(command.Descricao)));
        }

        if (command.Valor <= 0)
        {
            erros.Add(new Error(
                CodigoValorInvalido, "O valor deve ser maior que zero.", nameof(command.Valor)));
        }

        // A alteração não afrouxa a exigência da dupla data: limpar uma delas deixaria o
        // lançamento fora de uma das duas visões.
        if (command.DataCompetencia is null)
        {
            erros.Add(new Error(
                CodigoCompetenciaObrigatoria,
                "Informe a data em que o gasto aconteceu.",
                nameof(command.DataCompetencia)));
        }

        if (command.DataCaixa is null)
        {
            erros.Add(new Error(
                CodigoCaixaObrigatoria,
                "Informe a data em que o dinheiro sai.",
                nameof(command.DataCaixa)));
        }

        return erros;
    }
}
