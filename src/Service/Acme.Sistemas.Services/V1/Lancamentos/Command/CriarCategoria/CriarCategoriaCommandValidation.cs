using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.CriarCategoria;

public static class CriarCategoriaCommandValidation
{
    public const string CodigoNomeObrigatorio = "nome_obrigatorio";

    public const int TamanhoMaximoDoNome = 60;

    public const string CodigoNomeMuitoLongo = "nome_muito_longo";

    public static IReadOnlyList<Error> Validar(CriarCategoriaCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Nome))
        {
            return [new Error(CodigoNomeObrigatorio, "Informe o nome da categoria.", nameof(command.Nome))];
        }

        return command.Nome.Trim().Length > TamanhoMaximoDoNome
            ? [new Error(
                CodigoNomeMuitoLongo,
                $"O nome da categoria deve ter até {TamanhoMaximoDoNome} caracteres.",
                nameof(command.Nome))]
            : [];
    }
}
