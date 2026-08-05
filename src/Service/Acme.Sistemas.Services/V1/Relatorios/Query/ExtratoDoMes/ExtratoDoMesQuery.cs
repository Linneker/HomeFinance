using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Relatorios.Query.ExtratoDoMes;

/// <param name="PorCaixa">
/// Falso — o padrão — é competência, a visão primária do produto. A visão escolhida volta
/// explícita no resultado: sem isso, dois números diferentes para o mesmo mês pareceriam erro
/// do sistema em vez de duas leituras legítimas.
/// </param>
public sealed record ExtratoDoMesQuery(Guid TenantId, DateOnly Competencia, bool PorCaixa = false)
    : IRequest<ExtratoDoMesQueryResult>;
