using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Exportacao;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Repository.Helper;

namespace Acme.Sistemas.Repository.Repositories.V1.Exportacao.Query;

/// <summary>Materialização compartilhada entre as leituras de leitura e de escrita.</summary>
internal static class ExportacaoSolicitacaoMap
{
    public static ExportacaoSolicitacaoEntity Ler(DbDataReader leitor) => new()
    {
        Id = leitor.Identificador("id"),
        TenantId = leitor.Identificador("tenant_id"),
        UsuarioSolicitanteId = leitor.Identificador("usuario_solicitante_id"),
        Formato = (FormatoExportacao)leitor.GetInt16(leitor.GetOrdinal("formato")),
        Situacao = (SituacaoExportacao)leitor.GetInt16(leitor.GetOrdinal("situacao")),
        ReferenciaArquivo = TextoOpcional(leitor, "referencia_arquivo"),
        NomeArquivo = TextoOpcional(leitor, "nome_arquivo"),
        CriadoEm = leitor.DataHora("criado_em"),
        ConcluidoEm = leitor.DataHoraOpcional("concluido_em"),
        ExpiraEm = leitor.DataHoraOpcional("expira_em"),
        Erro = TextoOpcional(leitor, "erro"),
    };

    private static string? TextoOpcional(DbDataReader leitor, string coluna)
    {
        var ordinal = leitor.GetOrdinal(coluna);
        return leitor.IsDBNull(ordinal) ? null : leitor.GetString(ordinal);
    }
}
