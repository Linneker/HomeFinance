namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.CompartilharTenant;

/// <param name="IncluiScr">
/// Marcação explícita do item sensível. Ausente ou falso, o SCR fica fora — o padrão do
/// corpo JSON e o padrão do domínio precisam ser o mesmo, senão um cliente que omite o campo
/// compartilharia mais do que o dono viu na tela.
/// </param>
public sealed record CompartilharTenantRequest(
    string EmailDestinatario,
    bool ConsentimentoConfirmado,
    bool IncluiScr);
