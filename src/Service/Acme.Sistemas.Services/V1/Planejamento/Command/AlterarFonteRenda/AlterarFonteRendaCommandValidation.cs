using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.AlterarFonteRenda;

public static class AlterarFonteRendaCommandValidation
{
    public const string CodigoFonteObrigatoria = "fonte_renda_obrigatoria";
    public const string CodigoNomeObrigatorio = "nome_obrigatorio";
    public const string CodigoValorInvalido = "valor_invalido";
    public const string CodigoDiaInvalido = "dia_recebimento_invalido";

    public static IReadOnlyList<Error> Validar(AlterarFonteRendaCommand command)
    {
        var erros = new List<Error>();

        if (command.FonteId == Guid.Empty)
        {
            erros.Add(new Error(
                CodigoFonteObrigatoria,
                "Fonte de renda não identificada.",
                nameof(command.FonteId)));
        }

        if (string.IsNullOrWhiteSpace(command.Nome))
        {
            erros.Add(new Error(
                CodigoNomeObrigatorio,
                "Informe o nome da fonte de renda.",
                nameof(command.Nome)));
        }

        if (command.Valor <= 0)
        {
            erros.Add(new Error(
                CodigoValorInvalido,
                "O valor da renda deve ser maior que zero.",
                nameof(command.Valor)));
        }

        if (command.DiaRecebimento is < 1 or > 31)
        {
            erros.Add(new Error(
                CodigoDiaInvalido,
                "O dia de recebimento deve estar entre 1 e 31.",
                nameof(command.DiaRecebimento)));
        }

        return erros;
    }
}
