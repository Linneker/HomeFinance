using Acme.Sistemas.Domain.Entities.Classificacao;
using Acme.Sistemas.Domain.Interfaces.Repository;

namespace Acme.Sistemas.Services.V1.Classificacao;

/// <summary>
/// Escolhe a categoria de uma descrição a partir das regras do tenant.
/// </summary>
/// <remarks>
/// Quando mais de uma regra casa, vence a mais específica — o padrão mais longo — e o empate
/// resolve pela mais recente, porque a regra criada depois reflete a decisão mais atual da
/// pessoa. Sem uma ordem declarada, "IFOOD" e "IFOOD MERCADO" classificariam a mesma compra
/// de forma diferente conforme a ordem em que o banco devolvesse as linhas.
/// <para>
/// As regras vêm do repositório de escrita: a decisão faz parte da gravação do lançamento, e
/// contra réplica atrasada a regra que a pessoa acabou de criar não estaria lá — o lançamento
/// seguinte cairia em pendente e a correção manual pareceria não ter surtido efeito.
/// </para>
/// </remarks>
public sealed class ClassificacaoService(IRegraClassificacaoWriteRepository regras)
    : IClassificacaoService
{
    public async Task<ResultadoDaClassificacao> ClassificarAsync(
        string descricao,
        CancellationToken cancellationToken)
    {
        var normalizada = NormalizadorDeDescricao.Normalizar(descricao);

        if (normalizada.Length == 0)
        {
            return new ResultadoDaClassificacao(null);
        }

        var candidatas = await regras.ListarAsync(cancellationToken);

        // A consulta já devolve na ordem de precedência; a reordenação aqui é a rede contra
        // um repositório que mude a cláusula ORDER BY sem perceber que ela é regra de negócio.
        var vencedora = candidatas
            .Where(regra => regra.Casa(normalizada))
            .OrderByDescending(regra => regra.Especificidade)
            .ThenByDescending(regra => regra.CriadoEm)
            .FirstOrDefault();

        return new ResultadoDaClassificacao(vencedora);
    }
}
