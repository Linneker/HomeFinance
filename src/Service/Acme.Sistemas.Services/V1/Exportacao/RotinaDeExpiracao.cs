using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Exportacao;

public sealed record ResultadoDaExpiracao(int Removidos);

/// <summary>
/// Remove os arquivos de exportação cujo prazo passou.
/// </summary>
/// <remarks>
/// Roda sem tenant selecionado e varre todos: a limpeza não pode depender de o dono abrir o app.
/// Enumera pela função <c>SECURITY DEFINER</c>, apaga o objeto no armazenamento e só então marca
/// a solicitação como expirada — nessa ordem, porque marcar antes de apagar deixaria um arquivo
/// órfão vivo se a remoção falhasse.
/// </remarks>
public sealed class RotinaDeExpiracao(
    IExportacaoSolicitacaoWriteRepository solicitacoes,
    IArmazenamentoArquivos armazenamento,
    TimeProvider relogio,
    ILogger<RotinaDeExpiracao> logger)
{
    public async Task<ResultadoDaExpiracao> ExecutarAsync(CancellationToken cancellationToken)
    {
        var expiradas = await solicitacoes.ExpiradasAsync(relogio.GetUtcNow(), cancellationToken);

        var removidos = 0;

        foreach (var expirada in expiradas)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (expirada.ReferenciaArquivo is not null)
                {
                    await armazenamento.RemoverAsync(expirada.ReferenciaArquivo, cancellationToken);
                }

                await solicitacoes.ExpirarAsync(expirada.Id, cancellationToken);
                removidos++;
            }
            catch (Exception excecao) when (excecao is not OperationCanceledException)
            {
                // Uma falha num arquivo não pode travar a limpeza dos demais: a rotina roda
                // periodicamente e o que ficou para trás é reprocessado na próxima passagem.
                logger.LogError(excecao, "Falha ao expirar a exportação {Solicitacao}.", expirada.Id);
            }
        }

        logger.LogInformation("Expiração de exportações: {Removidos} arquivo(s) removido(s).", removidos);

        return new ResultadoDaExpiracao(removidos);
    }
}
