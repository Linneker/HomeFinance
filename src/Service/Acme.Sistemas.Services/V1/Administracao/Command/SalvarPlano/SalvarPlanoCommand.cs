using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Administracao.Command.SalvarPlano;

/// <summary>
/// Cria ou atualiza um plano do catálogo, incluindo a cota de compartilhamento — ação da operação.
/// </summary>
/// <remarks>O código identifica o plano: informar um código existente atualiza; um novo cria.</remarks>
public sealed record SalvarPlanoCommand(
    string Codigo,
    string Nome,
    int CotaCompartilhamentos,
    decimal PrecoMensal,
    bool Ativo)
    : IRequest<SalvarPlanoCommandResult>;
