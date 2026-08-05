using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.CadastrarFonteRenda;

public static class CadastrarFonteRendaCommandValidation
{
    public const string CodigoNomeObrigatorio = "nome_obrigatorio";
    public const string CodigoValorInvalido = "valor_invalido";
    public const string CodigoDiaInvalido = "dia_recebimento_invalido";

    public static IReadOnlyList<Error> Validar(CadastrarFonteRendaCommand command)
    {
        var erros = new List<Error>();

        if (string.IsNullOrWhiteSpace(command.Nome))
        {
            erros.Add(new Error(
                CodigoNomeObrigatorio,
                "Informe o nome da fonte de renda.",
                nameof(command.Nome)));
        }

        // Zero também é recusado: fonte que não traz dinheiro não muda a renda prevista e só
        // ocupa a tela. Quem parou de receber encerra a vigência, não zera o valor.
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
