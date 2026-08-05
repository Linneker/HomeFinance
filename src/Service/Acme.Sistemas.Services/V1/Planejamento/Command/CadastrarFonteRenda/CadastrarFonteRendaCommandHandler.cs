using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Entities.Planejamento;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.CadastrarFonteRenda;

/// <summary>
/// Cadastra uma fonte de renda com vigência aberta a partir de um mês.
/// </summary>
/// <remarks>
/// Não há verificação de nome repetido: duas fontes podem legitimamente se chamar "Salário"
/// quando a pessoa tem dois empregos, e é justamente a soma delas que forma a renda do mês.
/// </remarks>
public sealed class CadastrarFonteRendaCommandHandler(
    IFonteRendaWriteRepository fontes,
    TimeProvider relogio)
    : IRequestHandler<CadastrarFonteRendaCommand, CadastrarFonteRendaCommandResult>
{
    public async Task<CadastrarFonteRendaCommandResult> HandleAsync(
        CadastrarFonteRendaCommand request,
        CancellationToken cancellationToken)
    {
        var erros = CadastrarFonteRendaCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return CadastrarFonteRendaCommandResult.Recusada(erros[0]);
        }

        var vigencia = CalendarioFinanceiro.PrimeiroDiaDoMes(request.VigenteAPartirDe);

        var fonte = new FonteRendaEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = request.TenantId,
            Nome = request.Nome.Trim(),
            Valor = request.Valor,
            DiaRecebimento = request.DiaRecebimento,
            VigenciaInicio = vigencia,
            VigenciaFim = null,
            CriadoEm = relogio.GetUtcNow(),
        };

        await fontes.GravarAsync(fonte, cancellationToken);

        // Relê em vez de somar em memória: a renda do mês pode incluir fontes cadastradas em
        // outra sessão, e devolver um total incompleto faria a tela mostrar menos do que existe.
        var vigentes = await fontes.ListarAsync(vigencia, cancellationToken);

        return CadastrarFonteRendaCommandResult.Cadastrada(
            fonte.Id, vigencia, PrevisaoDeRenda.DoMes(vigencia, vigentes));
    }
}
