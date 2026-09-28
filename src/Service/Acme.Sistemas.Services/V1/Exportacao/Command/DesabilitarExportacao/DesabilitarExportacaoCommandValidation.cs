using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Exportacao.Command.DesabilitarExportacao;

public static class DesabilitarExportacaoCommandValidation
{
    public const string CodigoContaObrigatoria = "conta_obrigatoria";
    public const string CodigoMotivoObrigatorio = "motivo_obrigatorio";

    public static IReadOnlyList<Error> Validar(DesabilitarExportacaoCommand command)
    {
        var erros = new List<Error>();

        if (command.ContaUsuarioId == Guid.Empty)
        {
            erros.Add(new Error(CodigoContaObrigatoria, "Conta não identificada.", nameof(command.ContaUsuarioId)));
        }

        if (string.IsNullOrWhiteSpace(command.Motivo))
        {
            erros.Add(new Error(CodigoMotivoObrigatorio, "Informe o motivo da desabilitação.", nameof(command.Motivo)));
        }

        return erros;
    }
}
