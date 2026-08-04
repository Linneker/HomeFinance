using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Classificacao.Command.AlterarRegra;

public static class AlterarRegraCommandValidation
{
    public const string CodigoRegraObrigatoria = "regra_obrigatoria";
    public const string CodigoPadraoObrigatorio = "padrao_obrigatorio";
    public const string CodigoPadraoMuitoCurto = "padrao_muito_curto";
    public const string CodigoCategoriaObrigatoria = "categoria_obrigatoria";

    /// <summary>
    /// Padrão de uma ou duas letras casaria com quase toda descrição e classificaria o tenant
    /// inteiro de uma vez. Três é curto o bastante para siglas reais e longo o bastante para
    /// não ser acidente.
    /// </summary>
    public const int TamanhoMinimoDoPadrao = 3;

    public static IReadOnlyList<Error> Validar(AlterarRegraCommand command)
    {
        var erros = new List<Error>();

        if (command.RegraId == Guid.Empty)
        {
            erros.Add(new Error(
                CodigoRegraObrigatoria, "Regra não identificada.", nameof(command.RegraId)));
        }

        if (string.IsNullOrWhiteSpace(command.Padrao))
        {
            erros.Add(new Error(
                CodigoPadraoObrigatorio, "Informe o texto procurado.", nameof(command.Padrao)));
        }
        else if (command.Padrao.Trim().Length < TamanhoMinimoDoPadrao)
        {
            erros.Add(new Error(
                CodigoPadraoMuitoCurto,
                $"O texto procurado deve ter ao menos {TamanhoMinimoDoPadrao} caracteres.",
                nameof(command.Padrao)));
        }

        if (command.CategoriaId == Guid.Empty)
        {
            erros.Add(new Error(
                CodigoCategoriaObrigatoria, "Informe a categoria.", nameof(command.CategoriaId)));
        }

        return erros;
    }
}
