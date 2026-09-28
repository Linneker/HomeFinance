using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Entities.Auditoria;
using Acme.Sistemas.Domain.Entities.Exportacao;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.FeatureFlags;
using Acme.Sistemas.Infrastructure.Messaging;
using Acme.Sistemas.Services.V1.Exportacao.Formatadores;

namespace Acme.Sistemas.Services.V1.Exportacao.Command.SolicitarExportacao;

/// <summary>
/// Registra a solicitação e a enfileira para geração assíncrona.
/// </summary>
/// <remarks>
/// As duas travas em série aparecem aqui: a feature flag desliga o recurso para todos, a
/// habilitação o libera conta a conta. Ambas precisam estar ligadas. A recusa por tenant
/// compartilhado vem antes da habilitação — mesmo que a conta do destinatário esteja habilitada,
/// ele não exporta o tenant que apenas recebeu.
/// </remarks>
public sealed class SolicitarExportacaoCommandHandler(
    IFeatureFlagService flags,
    IExportacaoHabilitacaoWriteRepository habilitacoes,
    IResolvedorDeFormato formatos,
    IExportacaoSolicitacaoWriteRepository solicitacoes,
    IFilaExportacao fila,
    IAuditoriaRepository auditoria,
    TimeProvider relogio)
    : IRequestHandler<SolicitarExportacaoCommand, SolicitarExportacaoCommandResult>
{
    public const string CodigoRecursoIndisponivel = "exportacao_indisponivel";
    public const string CodigoTenantCompartilhado = "exportacao_tenant_compartilhado";
    public const string CodigoNaoHabilitada = "exportacao_nao_habilitada";
    public const string CodigoFormatoNaoSuportado = "formato_nao_suportado";

    public async Task<SolicitarExportacaoCommandResult> HandleAsync(
        SolicitarExportacaoCommand request,
        CancellationToken cancellationToken)
    {
        var erros = SolicitarExportacaoCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return SolicitarExportacaoCommandResult.Recusada(erros[0]);
        }

        // Interruptor global. Desligado, o recurso não existe para ninguém.
        if (!flags.Habilitada(FeatureFlags.ExportacaoDados))
        {
            return SolicitarExportacaoCommandResult.Recusada(
                new Error(CodigoRecursoIndisponivel, "A exportação não está disponível."));
        }

        // Dado de terceiro não se exporta, mesmo que a conta de quem pede esteja habilitada.
        if (request.SomenteLeitura)
        {
            return SolicitarExportacaoCommandResult.Recusada(
                new Error(CodigoTenantCompartilhado,
                    "Não é possível exportar os dados de um tenant recebido por compartilhamento."));
        }

        if (!await habilitacoes.HabilitadaAsync(request.UsuarioSolicitanteId, cancellationToken))
        {
            return SolicitarExportacaoCommandResult.Recusada(
                new Error(CodigoNaoHabilitada, "A exportação não está habilitada para esta conta."));
        }

        if (formatos.Resolver(request.Formato) is null)
        {
            return SolicitarExportacaoCommandResult.FormatoNaoSuportado(
                formatos.Disponiveis,
                new Error(CodigoFormatoNaoSuportado, "Formato de exportação não suportado.", nameof(request.Formato)));
        }

        var agora = relogio.GetUtcNow();

        var solicitacao = new ExportacaoSolicitacaoEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = request.TenantId,
            UsuarioSolicitanteId = request.UsuarioSolicitanteId,
            Formato = request.Formato,
            Situacao = SituacaoExportacao.Solicitada,
            CriadoEm = agora,
        };

        await solicitacoes.CriarAsync(solicitacao, cancellationToken);

        await fila.PublicarAsync(
            new MensagemExportacao(
                solicitacao.Id, request.TenantId, request.UsuarioSolicitanteId, (int)request.Formato),
            cancellationToken);

        await auditoria.RegistrarAsync(
            new RegistroAuditoriaEntity
            {
                Id = Guid.CreateVersion7(),
                Evento = EventoAuditoria.ExportacaoSolicitada,
                ContaUsuarioId = request.UsuarioSolicitanteId,
                AutorId = request.UsuarioSolicitanteId,
                Detalhe = $"formato={request.Formato}",
                CriadoEm = agora,
            },
            cancellationToken);

        return SolicitarExportacaoCommandResult.Solicitada(solicitacao.Id);
    }
}
