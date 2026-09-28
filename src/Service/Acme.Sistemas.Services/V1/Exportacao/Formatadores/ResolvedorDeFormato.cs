using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Services.V1.Exportacao.Formatadores;

/// <summary>Escolhe o formatador de um formato, ou recusa quando o formato não é oferecido.</summary>
public interface IResolvedorDeFormato
{
    /// <summary>O formatador do formato pedido, ou nulo se o sistema não o oferece.</summary>
    IFormatadorExportacao? Resolver(FormatoExportacao formato);

    /// <summary>Os formatos disponíveis — informados ao titular quando o pedido não é suportado.</summary>
    IReadOnlyList<FormatoExportacao> Disponiveis { get; }
}

/// <inheritdoc cref="IResolvedorDeFormato"/>
public sealed class ResolvedorDeFormato : IResolvedorDeFormato
{
    private readonly Dictionary<FormatoExportacao, IFormatadorExportacao> _porFormato;

    public ResolvedorDeFormato(IEnumerable<IFormatadorExportacao> formatadores)
    {
        _porFormato = formatadores.ToDictionary(formatador => formatador.Formato);
        Disponiveis = [.. _porFormato.Keys];
    }

    public IReadOnlyList<FormatoExportacao> Disponiveis { get; }

    public IFormatadorExportacao? Resolver(FormatoExportacao formato) =>
        _porFormato.GetValueOrDefault(formato);
}
