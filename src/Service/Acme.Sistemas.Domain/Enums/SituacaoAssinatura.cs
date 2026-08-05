namespace Acme.Sistemas.Domain.Enums;

/// <summary>
/// Situação da assinatura de uma conta.
/// </summary>
/// <remarks>
/// <see cref="Carencia"/> é estado próprio, e não uma suspensão com prazo: durante ela a conta
/// opera inteira. Fundir os dois tiraria os recursos derivados de quem só teve o cartão negado
/// numa tentativa — e recuperar cliente irritado custa mais do que os poucos dias de tolerância.
/// </remarks>
public enum SituacaoAssinatura
{
    Ativa = 1,

    /// <summary>Período pago venceu sem renovação confirmada. Opera como ativa até o prazo.</summary>
    Carencia = 2,

    /// <summary>Carência esgotada ou estorno. Recursos derivados param; o registro continua.</summary>
    Suspensa = 3,

    /// <summary>Encerrada em definitivo. Não volta — uma nova contratação cria outra assinatura.</summary>
    Cancelada = 4,
}
