namespace Acme.Sistemas.Core.Response.Erros;

/// <summary>
/// Resultado que sabe se construir como recusa.
/// </summary>
/// <remarks>
/// Existe para que um behavior genérico possa recusar sem conhecer o tipo concreto do
/// resultado. A alternativa seria lançar exceção do pipeline, e aí toda recusa de regra de
/// negócio chegaria ao cliente como erro de infraestrutura, indistinguível de uma falha real.
/// <para>
/// Cada resultado já tem sua fábrica de recusa com o nome que cabe ao seu contexto —
/// <c>Recusado</c>, <c>Recusada</c>. A implementação explícita liga as duas sem forçar
/// renomeação em massa nem expor um segundo membro público equivalente.
/// </para>
/// </remarks>
public interface IResultadoRecusavel<out TSelf>
{
    static abstract TSelf Recusar(Error erro);
}
