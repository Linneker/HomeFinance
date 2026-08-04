using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.CriarLancamento;

public static class CriarLancamentoCommandValidation
{
    public const string CodigoDescricaoObrigatoria = "descricao_obrigatoria";
    public const string CodigoValorInvalido = "valor_invalido";
    public const string CodigoCompetenciaObrigatoria = "data_competencia_obrigatoria";
    public const string CodigoCaixaObrigatoria = "data_caixa_obrigatoria";
    public const string CodigoParcelasInvalidas = "parcelas_invalidas";

    /// <summary>
    /// Teto de parcelas. Não é regra de negócio de emissor nenhum — é o limite acima do qual
    /// um erro de digitação geraria centenas de linhas antes de alguém perceber.
    /// </summary>
    public const int MaximoDeParcelas = 60;

    public static IReadOnlyList<Error> Validar(CriarLancamentoCommand command)
    {
        var erros = new List<Error>();

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

        // As duas datas são exigidas separadamente para que a mensagem diga qual está faltando.
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

        if (command.Parcelas is < 1 or > MaximoDeParcelas)
        {
            erros.Add(new Error(
                CodigoParcelasInvalidas,
                $"O número de parcelas deve ficar entre 1 e {MaximoDeParcelas}.",
                nameof(command.Parcelas)));
        }

        return erros;
    }
}
