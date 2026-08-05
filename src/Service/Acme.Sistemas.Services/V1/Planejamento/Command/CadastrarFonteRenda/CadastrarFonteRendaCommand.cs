using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.CadastrarFonteRenda;

/// <param name="VigenteAPartirDe">
/// Qualquer dia do primeiro mês em que a renda passa a valer. É reduzido ao primeiro dia do mês
/// antes de gravar — vigência de renda é mês, não dia.
/// </param>
public sealed record CadastrarFonteRendaCommand(
    Guid TenantId,
    string Nome,
    decimal Valor,
    int DiaRecebimento,
    DateOnly VigenteAPartirDe) : IRequest<CadastrarFonteRendaCommandResult>;
