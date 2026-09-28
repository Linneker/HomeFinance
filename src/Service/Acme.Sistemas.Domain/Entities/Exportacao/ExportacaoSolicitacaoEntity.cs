using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Domain.Entities.Exportacao;

/// <summary>
/// Uma solicitação de exportação dos dados do próprio tenant.
/// </summary>
/// <remarks>
/// Tem <c>tenant_id</c> e por isso vive sob RLS como todo dado do tenant — mas o download é
/// vinculado a <see cref="UsuarioSolicitanteId"/>, não ao tenant. A distinção importa: um
/// destinatário de compartilhamento enxerga o tenant, e sem esse vínculo poderia baixar o arquivo
/// que o dono gerou. Quem solicitou é quem baixa.
/// </remarks>
public sealed class ExportacaoSolicitacaoEntity : TenantEntityBase
{
    /// <summary>Conta que pediu a exportação. Só ela pode baixar o arquivo resultante.</summary>
    public Guid UsuarioSolicitanteId { get; set; }

    public FormatoExportacao Formato { get; set; }

    public SituacaoExportacao Situacao { get; set; }

    /// <summary>Chave do objeto no armazenamento. Nula até a geração concluir e após a expiração.</summary>
    public string? ReferenciaArquivo { get; set; }

    /// <summary>Nome amigável do arquivo entregue no download.</summary>
    public string? NomeArquivo { get; set; }

    public DateTimeOffset? ConcluidoEm { get; set; }

    /// <summary>Quando o arquivo deixa de estar disponível e é removido do armazenamento.</summary>
    public DateTimeOffset? ExpiraEm { get; set; }

    /// <summary>Motivo da falha, quando <see cref="Situacao"/> é <see cref="SituacaoExportacao.Falhou"/>.</summary>
    public string? Erro { get; set; }
}
