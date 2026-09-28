using Acme.Sistemas.Domain.Entities.Exportacao;
using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Services.V1.Exportacao.Formatadores;

/// <summary>Arquivo pronto: o conteúdo, o tipo para o download e a extensão do nome.</summary>
public sealed record ArquivoGerado(byte[] Conteudo, string ContentType, string Extensao);

/// <summary>
/// Transforma o conjunto de dados do tenant em um arquivo de um formato.
/// </summary>
/// <remarks>
/// Um formatador por formato, cada um dono do seu <see cref="Formato"/>. Acrescentar um formato
/// passa a ser criar uma implementação — o resolvedor a descobre por injeção, sem lista central a
/// manter.
/// </remarks>
public interface IFormatadorExportacao
{
    FormatoExportacao Formato { get; }

    ArquivoGerado Gerar(DadosDoTenantParaExportacao dados);
}
