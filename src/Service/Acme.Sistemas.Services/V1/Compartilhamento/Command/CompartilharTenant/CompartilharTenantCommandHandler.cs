using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;

/// <summary>
/// Concede acesso somente leitura ao tenant do dono.
/// </summary>
/// <remarks>
/// Só o dono compartilha: o tenant vem de <see cref="ITenantWriteRepository.ObterTenantDoUsuarioAsync"/>
/// a partir do usuário autenticado, nunca do pedido. É isso que impede quem só recebeu leitura
/// de repassar o acesso a um terceiro.
/// <para>
/// Todas as leituras usam o repositório de escrita: cada uma decide se a concessão acontece, e
/// contra réplica atrasada uma conta recém-criada ou um compartilhamento recém-revogado
/// produziriam a decisão errada.
/// </para>
/// </remarks>
public sealed class CompartilharTenantCommandHandler(
    ITenantWriteRepository tenants,
    TimeProvider relogio)
    : IRequestHandler<CompartilharTenantCommand, CompartilharTenantCommandResult>
{
    public const string CodigoTenantNaoEncontrado = "tenant_nao_encontrado";
    public const string CodigoDestinatarioNaoEncontrado = "destinatario_nao_encontrado";
    public const string CodigoCompartilhamentoConsigoMesmo = "compartilhamento_consigo_mesmo";
    public const string CodigoJaCompartilhado = "ja_compartilhado";
    public const string CodigoRepasseNaoPermitido = "repasse_nao_permitido";

    public async Task<CompartilharTenantCommandResult> HandleAsync(
        CompartilharTenantCommand request,
        CancellationToken cancellationToken)
    {
        var erros = CompartilharTenantCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return CompartilharTenantCommandResult.Recusado(erros[0]);
        }

        var tenantId = await tenants.ObterTenantDoUsuarioAsync(request.DonoUsuarioId, cancellationToken);

        if (tenantId is null)
        {
            return CompartilharTenantCommandResult.Recusado(
                new Error(CodigoTenantNaoEncontrado, "Tenant do titular não encontrado."));
        }

        if (request.TenantSelecionadoId is not null && request.TenantSelecionadoId != tenantId)
        {
            // Bruno, vendo o tenant da Ana, pede compartilhamento com Carla. Sem esta recusa
            // ele não repassaria o tenant da Ana — a linha acima já garante isso — mas
            // compartilharia o próprio sem perceber, achando que compartilhou o da Ana.
            return CompartilharTenantCommandResult.Recusado(
                new Error(
                    CodigoRepasseNaoPermitido,
                    "Só é possível compartilhar o seu próprio espaço. Selecione-o antes de continuar."));
        }

        // A busca do destinatário vem depois da recusa de repasse de propósito: responder
        // "não há conta com este e-mail" a quem nem podia compartilhar transformaria o
        // endpoint em consulta de existência de conta.
        var destinatarioId = await tenants.ObterUsuarioPorEmailAsync(
            request.EmailDestinatario, cancellationToken);

        if (destinatarioId is null)
        {
            return CompartilharTenantCommandResult.Recusado(
                new Error(
                    CodigoDestinatarioNaoEncontrado,
                    "Não há conta com este e-mail.",
                    nameof(request.EmailDestinatario)));
        }

        if (destinatarioId == request.DonoUsuarioId)
        {
            // Compartilhar consigo mesmo criaria um acesso somente leitura ao próprio tenant,
            // que confundiria a resolução de contexto sem beneficiar ninguém.
            return CompartilharTenantCommandResult.Recusado(
                new Error(CodigoCompartilhamentoConsigoMesmo, "Você já tem acesso ao seu tenant."));
        }

        if (await tenants.ExisteCompartilhamentoVigenteAsync(
                tenantId.Value, destinatarioId.Value, cancellationToken))
        {
            return CompartilharTenantCommandResult.Recusado(
                new Error(CodigoJaCompartilhado, "Este acesso já foi concedido."));
        }

        var agora = relogio.GetUtcNow();

        var compartilhamento = new TenantCompartilhamentoEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId.Value,
            UsuarioDestinatarioId = destinatarioId.Value,
            IncluiScr = request.IncluiScr,
            ConcedidoEm = agora,
            CriadoEm = agora,
        };

        await tenants.ConcederAsync(compartilhamento, cancellationToken);

        return CompartilharTenantCommandResult.Concedido(compartilhamento.Id);
    }
}
