namespace Acme.Sistemas.Infrastructure.FeatureFlags;

/// <summary>
/// Consulta de feature flags — padrão AutoProcess do blueprint Acme, com ativação em runtime.
/// </summary>
/// <remarks>
/// A flag é um interruptor global do recurso, não a autorização de uma conta. Exportação
/// desligada aqui esconde o recurso de todo mundo; ligada, ela ainda depende da habilitação
/// conta a conta. São duas travas em série de propósito: uma desliga o subsistema inteiro em
/// caso de incidente, a outra decide quem, dentro dele, pode usar.
/// </remarks>
public interface IFeatureFlagService
{
    /// <summary>Verdadeiro apenas quando a flag está explicitamente ligada. Ausência é desligado.</summary>
    bool Habilitada(string flag);
}
