using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.AlterarFonteRenda;

/// <param name="VigenteAPartirDe">
/// Mês a partir do qual o novo valor vale. Meses anteriores mantêm o valor antigo.
/// </param>
public sealed record AlterarFonteRendaCommand(
    Guid TenantId,
    Guid FonteId,
    string Nome,
    decimal Valor,
    int DiaRecebimento,
    DateOnly VigenteAPartirDe) : IRequest<AlterarFonteRendaCommandResult>;
