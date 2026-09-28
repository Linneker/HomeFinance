namespace Acme.Sistemas.Infrastructure.FeatureFlags;

/// <summary>Nomes das flags conhecidas. Const em vez de string solta evita divergência de grafia.</summary>
public static class FeatureFlags
{
    /// <summary>
    /// Exportação de dados do próprio tenant. Nasce desligada: o recurso só é oferecido depois
    /// de a operação habilitá-lo, e a flag é o interruptor que permite recolhê-lo de todos de
    /// uma vez sem republicar.
    /// </summary>
    public const string ExportacaoDados = "exportacao-dados";
}
