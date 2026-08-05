namespace Acme.Sistemas.Domain.Enums;

/// <summary>
/// Natureza do alerta.
/// </summary>
/// <remarks>
/// São problemas distintos e não se unificam. Estourar o orçamento de uma categoria é uma
/// questão de plano — o dinheiro pode até existir. Faltar saldo projetado é uma questão de
/// data — a conta pode estar dentro do plano e ainda assim não ter o dinheiro no dia certo.
/// Um aviso combinado esconderia justamente qual das duas coisas a pessoa precisa resolver.
/// </remarks>
public enum TipoAlerta
{
    /// <summary>Categoria que passou do valor planejado no mês.</summary>
    Competencia = 1,

    /// <summary>Saldo projetado insuficiente para as saídas até uma data.</summary>
    Caixa = 2,
}
