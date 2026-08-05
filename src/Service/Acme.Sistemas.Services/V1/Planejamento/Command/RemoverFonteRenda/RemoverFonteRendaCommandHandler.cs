using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.RemoverFonteRenda;

/// <summary>
/// Remove uma fonte de renda.
/// </summary>
/// <remarks>
/// Apaga de fato, e não encerra a vigência: encerrar é a operação de quem parou de receber e
/// quer o histórico preservado; remover é a de quem cadastrou errado. São intenções diferentes
/// e a segunda existe porque, sem ela, um cadastro duplicado ficaria para sempre inflando a
/// renda prevista de todos os meses passados.
/// </remarks>
public sealed class RemoverFonteRendaCommandHandler(IFonteRendaWriteRepository fontes)
    : IRequestHandler<RemoverFonteRendaCommand, RemoverFonteRendaCommandResult>
{
    public const string CodigoFonteNaoEncontrada = "fonte_renda_nao_encontrada";

    public async Task<RemoverFonteRendaCommandResult> HandleAsync(
        RemoverFonteRendaCommand request,
        CancellationToken cancellationToken)
    {
        var erros = RemoverFonteRendaCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return RemoverFonteRendaCommandResult.Recusada(erros[0]);
        }

        return await fontes.RemoverAsync(request.FonteId, cancellationToken)
            ? RemoverFonteRendaCommandResult.Removida()
            : RemoverFonteRendaCommandResult.Recusada(
                new Error(CodigoFonteNaoEncontrada, "Fonte de renda não encontrada."));
    }
}
