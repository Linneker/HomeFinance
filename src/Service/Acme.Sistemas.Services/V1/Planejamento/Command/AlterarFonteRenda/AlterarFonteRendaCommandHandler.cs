using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Entities.Planejamento;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.AlterarFonteRenda;

/// <summary>
/// Altera uma fonte de renda a partir de um mês, preservando os meses anteriores.
/// </summary>
/// <remarks>
/// São dois caminhos, e a diferença entre eles é se existe passado a proteger:
/// <list type="bullet">
/// <item>
/// O mês pedido é o mesmo em que a vigência começou (ou anterior): não há mês fechado com o
/// valor antigo, então a linha é corrigida no lugar. É o caso de quem digitou errado e
/// percebeu em seguida — abrir vigência nova aqui deixaria um registro de um mês que nunca
/// existiu na vida da pessoa.
/// </item>
/// <item>
/// O mês pedido é posterior: a vigência atual é fechada no mês anterior e uma nova é aberta.
/// É o aumento de salário, a mudança de contrato. Os relatórios já conferidos continuam
/// mostrando a renda que era a daquele mês.
/// </item>
/// </list>
/// </remarks>
public sealed class AlterarFonteRendaCommandHandler(
    IFonteRendaWriteRepository fontes,
    TimeProvider relogio)
    : IRequestHandler<AlterarFonteRendaCommand, AlterarFonteRendaCommandResult>
{
    public const string CodigoFonteNaoEncontrada = "fonte_renda_nao_encontrada";
    public const string CodigoVigenciaEncerrada = "fonte_renda_encerrada";

    public async Task<AlterarFonteRendaCommandResult> HandleAsync(
        AlterarFonteRendaCommand request,
        CancellationToken cancellationToken)
    {
        var erros = AlterarFonteRendaCommandValidation.Validar(request);

        if (erros.Count > 0)
        {
            return AlterarFonteRendaCommandResult.Recusada(erros[0]);
        }

        var fonte = await fontes.ObterAsync(request.FonteId, cancellationToken);

        if (fonte is null)
        {
            return AlterarFonteRendaCommandResult.Recusada(
                new Error(CodigoFonteNaoEncontrada, "Fonte de renda não encontrada."));
        }

        // Vigência fechada é histórico. Alterá-la mudaria meses que já foram conferidos, e é
        // exatamente o que o desenho de vigências existe para impedir.
        if (fonte.Encerrada)
        {
            return AlterarFonteRendaCommandResult.Recusada(
                new Error(
                    CodigoVigenciaEncerrada,
                    "Esta vigência já foi encerrada. Altere a vigência atual da fonte."));
        }

        var novaVigencia = CalendarioFinanceiro.PrimeiroDiaDoMes(request.VigenteAPartirDe);
        var nome = request.Nome.Trim();

        if (novaVigencia <= fonte.VigenciaInicio)
        {
            fonte.Nome = nome;
            fonte.Valor = request.Valor;
            fonte.DiaRecebimento = request.DiaRecebimento;

            return await fontes.AlterarAsync(fonte, cancellationToken)
                ? AlterarFonteRendaCommandResult.Alterada(fonte.Id, false, fonte.VigenciaInicio)
                : AlterarFonteRendaCommandResult.Recusada(
                    new Error(CodigoFonteNaoEncontrada, "Fonte de renda não encontrada."));
        }

        var sucessora = new FonteRendaEntity
        {
            Id = Guid.CreateVersion7(),
            TenantId = fonte.TenantId,
            Nome = nome,
            Valor = request.Valor,
            DiaRecebimento = request.DiaRecebimento,
            VigenciaInicio = novaVigencia,
            VigenciaFim = null,
            CriadoEm = relogio.GetUtcNow(),
        };

        fonte.EncerrarAntesDe(novaVigencia);

        await fontes.SucederAsync(fonte, sucessora, cancellationToken);

        return AlterarFonteRendaCommandResult.Alterada(sucessora.Id, true, novaVigencia);
    }
}
