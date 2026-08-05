namespace Acme.Sistemas.Core.Mediators.Handler;

/// <summary>
/// Marca um request como recurso que produz valor sobre os dados, e não como o registro deles.
/// </summary>
/// <remarks>
/// A distinção é o coração da política de inadimplência: registrar e ler os próprios lançamentos
/// continua funcionando com a assinatura suspensa, porque tirar o acesso ao que a pessoa digitou
/// seria reter o dado dela como refém. O que para é o que o produto produz em cima disso —
/// análise, alerta, orientação, compartilhamento.
/// <para>
/// Independente de <see cref="IEscritaFinanceira"/>: um request pode ser um, o outro, os dois ou
/// nenhum. Criar lançamento é escrita e não é derivado; o resumo por categoria é derivado e não
/// é escrita.
/// </para>
/// </remarks>
public interface IRecursoDerivado;
