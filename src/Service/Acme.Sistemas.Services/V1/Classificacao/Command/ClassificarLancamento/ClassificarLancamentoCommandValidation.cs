using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Classificacao.Command.ClassificarLancamento;

public static class ClassificarLancamentoCommandValidation
{
    public const string CodigoLancamentoObrigatorio = "lancamento_obrigatorio";
    public const string CodigoCategoriaObrigatoria = "categoria_obrigatoria";

    public static IReadOnlyList<Error> Validar(ClassificarLancamentoCommand command)
    {
        var erros = new List<Error>();

        if (command.LancamentoId == Guid.Empty)
        {
            erros.Add(new Error(
                CodigoLancamentoObrigatorio, "Lançamento não identificado.", nameof(command.LancamentoId)));
        }

        // Classificar exige categoria. Limpar a classificação é alteração de lançamento, não
        // classificação — juntar as duas coisas aqui esconderia a intenção de quem chama.
        if (command.CategoriaId == Guid.Empty)
        {
            erros.Add(new Error(
                CodigoCategoriaObrigatoria, "Informe a categoria.", nameof(command.CategoriaId)));
        }

        return erros;
    }
}
