using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Entities.Assinaturas;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Administracao.Command.SalvarPlano;

/// <summary>
/// Grava o plano: cria quando o código é novo, atualiza quando já existe.
/// </summary>
/// <remarks>
/// O plano existente conserva id e data de criação — o upsert é por código, e trocar o id
/// romperia as assinaturas que já apontam para ele. A escrita passa pela função definer do
/// catálogo; a autorização administrativa é imposta no endpoint.
/// </remarks>
public sealed class SalvarPlanoCommandHandler(
    IPlanoReadRepository planosLeitura,
    IPlanoWriteRepository planosEscrita,
    TimeProvider relogio)
    : IRequestHandler<SalvarPlanoCommand, SalvarPlanoCommandResult>
{
    public async Task<SalvarPlanoCommandResult> HandleAsync(
        SalvarPlanoCommand request,
        CancellationToken cancellationToken)
    {
        var erros = SalvarPlanoCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return SalvarPlanoCommandResult.Recusado(erros[0]);
        }

        var existente = await planosLeitura.ObterPorCodigoAsync(request.Codigo, cancellationToken);
        var criando = existente is null;

        var plano = new PlanoEntity
        {
            Id = existente?.Id ?? Guid.CreateVersion7(),
            Codigo = request.Codigo,
            Nome = request.Nome,
            CotaCompartilhamentos = request.CotaCompartilhamentos,
            PrecoMensal = request.PrecoMensal,
            Ativo = request.Ativo,
            CriadoEm = existente?.CriadoEm ?? relogio.GetUtcNow(),
        };

        await planosEscrita.SalvarAsync(plano, cancellationToken);

        return SalvarPlanoCommandResult.Salvo(plano.Id, criando);
    }
}
