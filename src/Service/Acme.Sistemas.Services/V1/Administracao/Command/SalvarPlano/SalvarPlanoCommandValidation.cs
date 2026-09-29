using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Administracao.Command.SalvarPlano;

public static class SalvarPlanoCommandValidation
{
    public const string CodigoObrigatorio = "codigo_obrigatorio";
    public const string NomeObrigatorio = "nome_obrigatorio";
    public const string CotaInvalida = "cota_invalida";
    public const string PrecoInvalido = "preco_invalido";

    public static IReadOnlyList<Error> Validar(SalvarPlanoCommand command)
    {
        var erros = new List<Error>();

        if (string.IsNullOrWhiteSpace(command.Codigo))
        {
            erros.Add(new Error(CodigoObrigatorio, "Informe o código do plano.", nameof(command.Codigo)));
        }

        if (string.IsNullOrWhiteSpace(command.Nome))
        {
            erros.Add(new Error(NomeObrigatorio, "Informe o nome do plano.", nameof(command.Nome)));
        }

        // Zero é cota legítima (plano sem compartilhamento); negativo, não.
        if (command.CotaCompartilhamentos < 0)
        {
            erros.Add(new Error(CotaInvalida, "A cota de compartilhamentos não pode ser negativa.", nameof(command.CotaCompartilhamentos)));
        }

        if (command.PrecoMensal < 0)
        {
            erros.Add(new Error(PrecoInvalido, "O preço mensal não pode ser negativo.", nameof(command.PrecoMensal)));
        }

        return erros;
    }
}
