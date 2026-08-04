using Acme.Sistemas.Domain.Entities.Classificacao;

namespace Acme.Sistemas.Services.V1.Classificacao;

/// <param name="Regra">Nula quando nenhuma regra do tenant casou com a descrição.</param>
public sealed record ResultadoDaClassificacao(RegraClassificacaoEntity? Regra)
{
    public bool Classificou => Regra is not null;

    public Guid? CategoriaId => Regra?.CategoriaId;
}

public interface IClassificacaoService
{
    /// <summary>Categoria que as regras do tenant atribuem à descrição, se alguma casar.</summary>
    Task<ResultadoDaClassificacao> ClassificarAsync(
        string descricao,
        CancellationToken cancellationToken);
}
