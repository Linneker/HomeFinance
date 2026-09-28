using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Messaging;
using Acme.Sistemas.Infrastructure.Storage;
using Acme.Sistemas.Services.V1.Alertas;
using Acme.Sistemas.Services.V1.Exportacao.Formatadores;
using Microsoft.Extensions.Logging;

namespace Acme.Sistemas.Services.V1.Exportacao;

/// <summary>
/// Monta o arquivo de uma exportação a partir de uma mensagem da fila.
/// </summary>
/// <remarks>
/// Roda fora de requisição, então assume o contexto do titular — como as rotinas de alerta e de
/// reconciliação. É esse contexto que faz a RLS entregar só os dados daquele tenant: a geração
/// nunca alcança dado de terceiro porque a própria leitura não o enxerga.
/// <para>
/// Falha marca a solicitação como <see cref="SituacaoExportacao.Falhou"/> em vez de estourar: o
/// titular vê o estado e pede de novo, e uma exceção propagada devolveria a mensagem à fila para
/// tentar reprocessar dados que talvez nunca formatem.
/// </para>
/// </remarks>
public sealed class GeradorDeExportacao(
    IContextoAssumivel contexto,
    IExportacaoSolicitacaoWriteRepository solicitacoes,
    IExportacaoDadosReadRepository dados,
    IResolvedorDeFormato formatos,
    IArmazenamentoArquivos armazenamento,
    IAvisoTitular avisos,
    TimeProvider relogio,
    ILogger<GeradorDeExportacao> logger)
{
    /// <summary>Prazo de disponibilidade do arquivo. Curto: é dado sensível, não fica hospedado.</summary>
    public static readonly TimeSpan Retencao = TimeSpan.FromDays(7);

    public async Task GerarAsync(MensagemExportacao mensagem, CancellationToken cancellationToken)
    {
        contexto.Assumir(mensagem.UsuarioId, mensagem.TenantId);

        var solicitacao = await solicitacoes.ObterAsync(mensagem.SolicitacaoId, cancellationToken);

        // Cancelada pela desabilitação, ou já processada: nada a fazer. Regenerar sobrescreveria
        // um cancelamento que a operação decidiu.
        if (solicitacao is null || solicitacao.Situacao != SituacaoExportacao.Solicitada)
        {
            logger.LogInformation(
                "Exportação {Solicitacao} ignorada: ausente ou não está mais solicitada.",
                mensagem.SolicitacaoId);
            return;
        }

        solicitacao.Situacao = SituacaoExportacao.Gerando;
        await solicitacoes.AtualizarAsync(solicitacao, cancellationToken);

        try
        {
            var formatador = formatos.Resolver(solicitacao.Formato)
                ?? throw new InvalidOperationException($"Formato {solicitacao.Formato} sem formatador.");

            var conjunto = await dados.ColetarAsync(cancellationToken);
            var arquivo = formatador.Gerar(conjunto);

            var chave = $"exportacoes/{mensagem.TenantId:N}/{mensagem.SolicitacaoId:N}.{arquivo.Extensao}";
            await armazenamento.GravarAsync(chave, arquivo.Conteudo, arquivo.ContentType, cancellationToken);

            var agora = relogio.GetUtcNow();
            solicitacao.Situacao = SituacaoExportacao.Concluida;
            solicitacao.ReferenciaArquivo = chave;
            solicitacao.NomeArquivo = $"homefinance-{agora:yyyyMMdd}.{arquivo.Extensao}";
            solicitacao.ConcluidoEm = agora;
            solicitacao.ExpiraEm = agora.Add(Retencao);
            await solicitacoes.AtualizarAsync(solicitacao, cancellationToken);

            await avisos.AvisarAsync(
                mensagem.UsuarioId,
                "Exportação pronta",
                "O arquivo com os seus dados está disponível para download.",
                cancellationToken);
        }
        catch (Exception excecao) when (excecao is not OperationCanceledException)
        {
            logger.LogError(excecao, "Falha ao gerar a exportação {Solicitacao}.", mensagem.SolicitacaoId);

            solicitacao.Situacao = SituacaoExportacao.Falhou;
            solicitacao.Erro = excecao.Message;
            await solicitacoes.AtualizarAsync(solicitacao, cancellationToken);
        }
    }
}
