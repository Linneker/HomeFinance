using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Entities.Planejamento;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.DefinirOrcamento;

/// <summary>
/// Define quanto a pessoa pretende gastar numa categoria no mês.
/// </summary>
/// <remarks>
/// Devolve o total do mês e a renda prevista junto com o resultado. É o que permite à tela
/// mostrar o efeito da linha recém-editada sem uma segunda chamada — e é também o que torna o
/// aviso de excesso possível no mesmo instante da gravação, em vez de só na próxima abertura
/// da tela.
/// </remarks>
public sealed class DefinirOrcamentoCommandHandler(
    IOrcamentoWriteRepository orcamentos,
    ICategoriaWriteRepository categorias,
    IFonteRendaWriteRepository fontes,
    TimeProvider relogio)
    : IRequestHandler<DefinirOrcamentoCommand, DefinirOrcamentoCommandResult>
{
    public const string CodigoCategoriaInexistente = "categoria_inexistente";

    public async Task<DefinirOrcamentoCommandResult> HandleAsync(
        DefinirOrcamentoCommand request,
        CancellationToken cancellationToken)
    {
        var erros = DefinirOrcamentoCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return DefinirOrcamentoCommandResult.Recusado(erros[0]);
        }

        if (!await categorias.ExisteAsync(request.CategoriaId, cancellationToken))
        {
            return DefinirOrcamentoCommandResult.Recusado(
                new Error(
                    CodigoCategoriaInexistente,
                    "Categoria não encontrada neste espaço.",
                    nameof(request.CategoriaId)));
        }

        var competencia = CalendarioFinanceiro.PrimeiroDiaDoMes(request.Competencia);

        var orcamento = new OrcamentoMensalEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = request.TenantId,
            CategoriaId = request.CategoriaId,
            Competencia = competencia,
            ValorPlanejado = request.ValorPlanejado,
            CriadoEm = relogio.GetUtcNow(),
        };

        await orcamentos.GravarAsync(orcamento, cancellationToken);

        var doMes = await orcamentos.ListarDoMesAsync(competencia, cancellationToken);
        var vigentes = await fontes.ListarAsync(competencia, cancellationToken);

        var total = doMes.Sum(item => item.ValorPlanejado);
        var renda = PrevisaoDeRenda.DoMes(competencia, vigentes);

        return DefinirOrcamentoCommandResult.Definido(
            orcamento.Id,
            competencia,
            total,
            renda,
            PrevisaoDeRenda.Avaliar(renda, total)?.Mensagem);
    }
}
