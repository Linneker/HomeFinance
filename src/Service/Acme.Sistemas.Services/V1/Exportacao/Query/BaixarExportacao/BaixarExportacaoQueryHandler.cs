using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Domain.Entities.Auditoria;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Storage;

namespace Acme.Sistemas.Services.V1.Exportacao.Query.BaixarExportacao;

/// <summary>
/// Entrega o arquivo, e só a quem tem direito a ele.
/// </summary>
/// <remarks>
/// Três recusas distintas, e a ordem importa: primeiro "não é sua" (outra conta), depois
/// "não está pronta ou expirou". Baixar registra auditoria — o evento de download faz parte da
/// trilha tanto quanto a solicitação.
/// </remarks>
public sealed class BaixarExportacaoQueryHandler(
    IExportacaoSolicitacaoReadRepository solicitacoes,
    IArmazenamentoArquivos armazenamento,
    IAuditoriaRepository auditoria,
    TimeProvider relogio)
    : IRequestHandler<BaixarExportacaoQuery, BaixarExportacaoQueryResult>
{
    public const string CodigoNaoEncontrada = "exportacao_nao_encontrada";
    public const string CodigoDeOutraConta = "download_de_outra_conta";
    public const string CodigoIndisponivel = "exportacao_indisponivel";

    public async Task<BaixarExportacaoQueryResult> HandleAsync(
        BaixarExportacaoQuery request,
        CancellationToken cancellationToken)
    {
        if (BaixarExportacaoQueryValidation.Validar(request).Count > 0)
        {
            return BaixarExportacaoQueryResult.Recusado(
                new Error(CodigoNaoEncontrada, "Exportação não encontrada."));
        }

        var solicitacao = await solicitacoes.ObterAsync(request.SolicitacaoId, cancellationToken);

        if (solicitacao is null)
        {
            return BaixarExportacaoQueryResult.Recusado(
                new Error(CodigoNaoEncontrada, "Exportação não encontrada."));
        }

        // Vínculo à conta solicitante — antes de qualquer coisa sobre o estado do arquivo.
        if (solicitacao.UsuarioSolicitanteId != request.UsuarioId)
        {
            return BaixarExportacaoQueryResult.Recusado(
                new Error(CodigoDeOutraConta, "Este arquivo pertence a outra conta."));
        }

        var expirada = solicitacao.ExpiraEm is not null && solicitacao.ExpiraEm <= relogio.GetUtcNow();

        if (solicitacao.Situacao != Acme.Sistemas.Domain.Enums.SituacaoExportacao.Concluida
            || solicitacao.ReferenciaArquivo is null
            || expirada)
        {
            return BaixarExportacaoQueryResult.Recusado(
                new Error(CodigoIndisponivel, "A exportação não está disponível para download."));
        }

        var conteudo = await armazenamento.BaixarAsync(solicitacao.ReferenciaArquivo, cancellationToken);

        if (conteudo is null)
        {
            // Referência viva no banco, arquivo ausente no storage — expirado entre a leitura e o
            // download, ou removido. Trata como indisponível, não como erro de sistema.
            return BaixarExportacaoQueryResult.Recusado(
                new Error(CodigoIndisponivel, "A exportação não está mais disponível."));
        }

        await auditoria.RegistrarAsync(
            new RegistroAuditoriaEntity
            {
                Id = Guid.CreateVersion7(),
                Evento = EventoAuditoria.ExportacaoBaixada,
                ContaUsuarioId = request.UsuarioId,
                AutorId = request.UsuarioId,
                Detalhe = $"solicitacao={solicitacao.Id}",
                CriadoEm = relogio.GetUtcNow(),
            },
            cancellationToken);

        return BaixarExportacaoQueryResult.Baixado(
            conteudo,
            ContentTypeDe(solicitacao.Formato),
            solicitacao.NomeArquivo ?? $"exportacao.{ExtensaoDe(solicitacao.Formato)}");
    }

    private static string ContentTypeDe(FormatoExportacao formato) => formato switch
    {
        FormatoExportacao.Csv => "text/csv",
        FormatoExportacao.Json => "application/json",
        _ => "text/plain; charset=utf-8",
    };

    private static string ExtensaoDe(FormatoExportacao formato) => formato switch
    {
        FormatoExportacao.Csv => "csv",
        FormatoExportacao.Json => "json",
        _ => "txt",
    };
}
