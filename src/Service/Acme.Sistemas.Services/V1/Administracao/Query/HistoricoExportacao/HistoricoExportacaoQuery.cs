using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Administracao.Query.HistoricoExportacao;

/// <summary>Histórico de habilitação/desabilitação da exportação de uma conta, para o back-office.</summary>
public sealed record HistoricoExportacaoQuery(Guid ContaUsuarioId)
    : IRequest<HistoricoExportacaoQueryResult>;
