using Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;
using Acme.Sistemas.Services.V1.Compartilhamento.Command.RevogarCompartilhamento;
using Acme.Sistemas.Services.V1.Compartilhamento.Query.ItensDoConsentimento;

namespace Acme.Sistemas.HomeFinance.Api.Endpoints.V1.CompartilharTenant;

internal static class CompartilharTenantMap
{
    /// <summary>
    /// O titular e o tenant ativo vêm do contexto, nunca do corpo: aceitá-los do cliente
    /// permitiria compartilhar o espaço de outra pessoa informando o identificador dela.
    /// </summary>
    public static CompartilharTenantCommand ParaCommand(
        this CompartilharTenantRequest request, Guid usuarioId, Guid? tenantSelecionadoId) =>
        new(usuarioId,
            tenantSelecionadoId,
            request.EmailDestinatario,
            request.ConsentimentoConfirmado,
            request.IncluiScr);

    public static RevogarCompartilhamentoCommand ParaCommandRevogacao(
        Guid usuarioId, Guid? tenantSelecionadoId, Guid destinatarioId) =>
        new(usuarioId, tenantSelecionadoId, destinatarioId);

    public static CompartilharTenantResponse ParaResponse(this CompartilharTenantCommandResult resultado) =>
        new(resultado.CompartilhamentoId!.Value);

    public static ItensDoConsentimentoResponse ParaResponse(this ItensDoConsentimentoQueryResult resultado) =>
        new([.. resultado.Itens.Select(i =>
            new ItemDeConsentimentoResponse(i.Chave, i.Titulo, i.Descricao, i.Sensivel, i.MarcadoPorPadrao))]);
}
