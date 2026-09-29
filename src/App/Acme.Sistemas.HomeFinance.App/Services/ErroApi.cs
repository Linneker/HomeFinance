namespace Acme.Sistemas.HomeFinance.App.Services;

/// <summary>Um erro de domínio devolvido pela Api, com o código que o app usa para reagir.</summary>
public sealed record ErroDominio(string Codigo, string Mensagem, string? Campo);

/// <summary>
/// Falha de uma chamada à Api. Carrega o status HTTP e, quando há, o primeiro código de domínio.
/// </summary>
public sealed class ErroApi(int status, IReadOnlyList<ErroDominio> erros, string mensagem)
    : Exception(mensagem)
{
    public int Status { get; } = status;

    public IReadOnlyList<ErroDominio> Erros { get; } = erros;

    /// <summary>Primeiro código de domínio, quando a resposta o trouxe.</summary>
    public string? Codigo => Erros.Count > 0 ? Erros[0].Codigo : null;

    /// <summary>Sessão expirada ou ausente: a UI deve voltar ao login.</summary>
    public bool NaoAutorizado => Status == 401;
}
