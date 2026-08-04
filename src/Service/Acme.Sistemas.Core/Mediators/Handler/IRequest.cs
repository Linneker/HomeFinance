namespace Acme.Sistemas.Core.Mediators.Handler;

/// <summary>
/// Marca uma operação que produz retorno. Commands e Queries implementam esta interface;
/// o que os diferencia é a intenção e a conexão de banco que o repositório usa, não o contrato.
/// </summary>
public interface IRequest<out TResponse>;
