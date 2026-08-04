using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.CriarRecorrencia;

public static class CriarRecorrenciaCommandValidation
{
    public const string CodigoDescricaoObrigatoria = "descricao_obrigatoria";
    public const string CodigoValorInvalido = "valor_invalido";
    public const string CodigoDiaInvalido = "dia_do_mes_invalido";
    public const string CodigoInicioObrigatorio = "inicio_obrigatorio";

    public static IReadOnlyList<Error> Validar(CriarRecorrenciaCommand command)
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

        // Aceita 29, 30 e 31: quem paga no último dia do mês precisa poder dizer isso. Quem
        // recua para o fim do mês curto é a materialização.
        if (command.DiaDoMes is < 1 or > 31)
        {
            erros.Add(new Error(
                CodigoDiaInvalido, "O dia do mês deve ficar entre 1 e 31.", nameof(command.DiaDoMes)));
        }

        if (command.Inicio is null)
        {
            erros.Add(new Error(
                CodigoInicioObrigatorio, "Informe a partir de quando repete.", nameof(command.Inicio)));
        }

        return erros;
    }
}
