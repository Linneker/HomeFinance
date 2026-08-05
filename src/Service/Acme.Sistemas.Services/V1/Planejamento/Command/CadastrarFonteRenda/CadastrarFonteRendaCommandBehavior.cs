using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Domain.Entities.Onboarding;
using Acme.Sistemas.Services.V1.Onboarding;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.CadastrarFonteRenda;

/// <summary>
/// Registro do cadastro e cumprimento do requisito bloqueante de renda declarada.
/// </summary>
/// <remarks>
/// É aqui que o produto destrava. O requisito é cumprido pela ação real que ele representa —
/// não há rota para marcá-lo à mão, o que impede destravar a conta sem declarar renda nenhuma.
/// </remarks>
public sealed class CadastrarFonteRendaCommandBehavior(
    ILogger<CadastrarFonteRendaCommandBehavior> logger,
    IOnboardingService onboarding)
    : IPipelineBehavior<CadastrarFonteRendaCommand, CadastrarFonteRendaCommandResult>
{
    public async Task<CadastrarFonteRendaCommandResult> HandleAsync(
        CadastrarFonteRendaCommand request,
        RequestHandlerDelegate<CadastrarFonteRendaCommandResult> next,
        CancellationToken cancellationToken)
    {
        var resultado = await next();

        if (resultado.Sucesso)
        {
            // Sem valor e sem nome: renda é dado sensível e não vai para log.
            logger.LogInformation(
                "Fonte de renda cadastrada com vigência a partir de {Vigencia}.",
                resultado.VigenciaInicio);

            // Repetir no segundo cadastro é inofensivo: a gravação não move a data do primeiro
            // cumprimento, que é o registro de quando a conta destravou.
            await onboarding.CumprirAsync(
                OnboardingRequisitoEntity.CodigoRendaDeclarada, cancellationToken);
        }

        return resultado;
    }
}
