using System.Text.Json;

namespace Acme.Sistemas.Infrastructure.FeatureFlags;

/// <inheritdoc cref="IFeatureFlagService"/>
/// <remarks>
/// Lê o <c>featureflags.json</c> a cada consulta. É o que dá ativação em runtime sem reiniciar
/// a aplicação: editar o arquivo (ou o ConfigMap que o monta) muda o comportamento na próxima
/// requisição. O custo de um <c>File.ReadAllText</c> é irrelevante perto de qualquer chamada de
/// banco, e o arquivo é pequeno.
/// <para>
/// Qualquer falha — arquivo ausente, JSON quebrado, chave inexistente — resolve como
/// <c>false</c>. Uma flag de recurso sensível tem que falhar fechada: ligar por engano um
/// recurso de exportação de dados financeiros porque o arquivo não parseou seria o pior default
/// possível.
/// </para>
/// </remarks>
public sealed class FeatureFlagService(string caminhoArquivo) : IFeatureFlagService
{
    /// <summary>Seção que agrupa as flags no arquivo, alinhada ao padrão de configuração.</summary>
    private const string Secao = "FeatureFlags";

    public bool Habilitada(string flag)
    {
        try
        {
            if (!File.Exists(caminhoArquivo))
            {
                return false;
            }

            using var documento = JsonDocument.Parse(File.ReadAllText(caminhoArquivo));

            return documento.RootElement.TryGetProperty(Secao, out var secao)
                   && secao.TryGetProperty(flag, out var valor)
                   && valor.ValueKind == JsonValueKind.True;
        }
        catch
        {
            // Falha fechada: sem certeza de que a flag está ligada, o recurso permanece invisível.
            return false;
        }
    }
}
