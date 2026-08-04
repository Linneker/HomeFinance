namespace Acme.Sistemas.Domain.Enums;

/// <summary>
/// De onde veio a categoria do lançamento.
/// </summary>
/// <remarks>
/// Guardar a origem é o que permite cumprir a regra de que classificação automática nunca
/// sobrescreve escolha manual, sem manter histórico completo de alterações. Os valores são
/// os mesmos gravados na coluna <c>origem_classificacao</c> e não podem ser renumerados.
/// </remarks>
public enum OrigemClassificacao
{
    /// <summary>Sem categoria. Entra na fila de classificação pendente.</summary>
    Pendente = 0,

    /// <summary>Atribuída por uma regra de classificação.</summary>
    Regra = 1,

    /// <summary>Escolhida pela pessoa. Regra nenhuma sobrescreve.</summary>
    Manual = 2,
}
