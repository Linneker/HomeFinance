using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Exportacao.Command.HabilitarExportacao;

public static class HabilitarExportacaoCommandValidation
{
    public const string CodigoContaObrigatoria = "conta_obrigatoria";
    public const string CodigoMotivoObrigatorio = "motivo_obrigatorio";

    public static IReadOnlyList<Error> Validar(HabilitarExportacaoCommand command)
    {
        var erros = new List<Error>();

        if (command.ContaUsuarioId == Guid.Empty)
        {
            erros.Add(new Error(CodigoContaObrigatoria, "Conta não identificada.", nameof(command.ContaUsuarioId)));
        }

        // Sem motivo não se habilita: a exportação abre acesso ao dado mais sensível, e a razão
        // registrada é o que torna a concessão auditável depois.
        if (string.IsNullOrWhiteSpace(command.Motivo))
        {
            erros.Add(new Error(CodigoMotivoObrigatorio, "Informe o motivo da habilitação.", nameof(command.Motivo)));
        }

        return erros;
    }
}
