namespace Acme.Sistemas.Core.Response.Erros;

/// <summary>
/// Erro devolvido ao cliente. <paramref name="Codigo"/> é estável e serve para o cliente
/// decidir comportamento; <paramref name="Mensagem"/> é texto para pessoa e pode mudar.
/// </summary>
public sealed record Error(string Codigo, string Mensagem, string? Campo = null);
