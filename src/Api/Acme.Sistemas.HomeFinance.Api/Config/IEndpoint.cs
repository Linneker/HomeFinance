using System.Reflection;

namespace Acme.Sistemas.HomeFinance.Api.Config;

/// <summary>Um endpoint por arquivo: rota, verbo e handler declarados juntos.</summary>
public interface IEndpoint
{
    void Mapear(IEndpointRouteBuilder rotas);
}

public static class EndpointExtensions
{
    /// <summary>
    /// Descobre e mapeia todos os <see cref="IEndpoint"/> do assembly. Acrescentar um endpoint
    /// passa a ser criar o arquivo — sem editar uma lista central que alguém esquece.
    /// </summary>
    public static void MapearEndpoints(this IEndpointRouteBuilder rotas)
    {
        var tipos = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(tipo => tipo is { IsAbstract: false, IsInterface: false }
                           && typeof(IEndpoint).IsAssignableFrom(tipo));

        foreach (var tipo in tipos)
        {
            ((IEndpoint)Activator.CreateInstance(tipo)!).Mapear(rotas);
        }
    }
}
