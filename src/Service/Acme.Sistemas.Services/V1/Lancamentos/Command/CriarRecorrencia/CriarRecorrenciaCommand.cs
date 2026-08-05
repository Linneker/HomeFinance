using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.CriarRecorrencia;

/// <param name="DiaDoMes">
/// Dia da ocorrência. Em mês mais curto a data recua para o último dia — dia 31 em fevereiro
/// não deixa de existir.
/// </param>
public sealed record CriarRecorrenciaCommand(
    Guid TenantId,
    Guid UsuarioId,
    string Descricao,
    decimal Valor,
    int DiaDoMes,
    Guid? CategoriaId,
    DateOnly? Inicio) : IRequest<CriarRecorrenciaCommandResult>, IEscritaFinanceira;
