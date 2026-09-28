using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Exportacao.Command.SolicitarExportacao;

public static class SolicitarExportacaoCommandValidation
{
    public const string CodigoTenantObrigatorio = "tenant_obrigatorio";
    public const string CodigoUsuarioObrigatorio = "usuario_obrigatorio";

    public static IReadOnlyList<Error> Validar(SolicitarExportacaoCommand command)
    {
        var erros = new List<Error>();

        if (command.TenantId == Guid.Empty)
        {
            erros.Add(new Error(CodigoTenantObrigatorio, "Tenant não identificado.", nameof(command.TenantId)));
        }

        if (command.UsuarioSolicitanteId == Guid.Empty)
        {
            erros.Add(new Error(CodigoUsuarioObrigatorio, "Titular não identificado.", nameof(command.UsuarioSolicitanteId)));
        }

        return erros;
    }
}
