namespace Acme.Sistemas.Domain.Enums;

/// <summary>
/// Ciclo de vida do alerta.
/// </summary>
/// <remarks>
/// <see cref="Dispensado"/> é estado próprio, e não um <c>Encerrado</c> com origem diferente:
/// dispensar significa "eu sei, não me avise de novo", e a condição continua valendo. Encerrar
/// significa que a condição deixou de existir. Confundir os dois faria o alerta reaparecer no
/// próximo lançamento que a pessoa registrasse.
/// </remarks>
public enum EstadoAlerta
{
    Aberto = 1,

    /// <summary>A condição deixou de existir. Só o sistema encerra.</summary>
    Encerrado = 2,

    /// <summary>A pessoa mandou parar de avisar. A condição segue valendo.</summary>
    Dispensado = 3,
}
