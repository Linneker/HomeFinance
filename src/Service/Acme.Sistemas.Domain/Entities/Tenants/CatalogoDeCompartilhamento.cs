namespace Acme.Sistemas.Domain.Entities.Tenants;

/// <param name="Sensivel">
/// Item que fica fora do compartilhamento por padrão e só entra por marcação explícita do dono.
/// </param>
public sealed record ItemCompartilhavel(string Chave, string Titulo, string Descricao, bool Sensivel)
{
    /// <summary>O que a tela deve trazer marcado ao abrir. Sensível nasce desmarcado.</summary>
    public bool MarcadoPorPadrao => !Sensivel;
}

/// <summary>
/// O que fica visível para quem recebe um tenant compartilhado, item a item.
/// </summary>
/// <remarks>
/// Configuração de produto, não dado de cliente: por isso vive aqui e não numa tabela. A tela
/// de consentimento é montada a partir desta lista, e é ela que dá sentido ao "consentimento
/// itemizado" — sem enumerar o que será visto, a confirmação do dono não prova nada.
/// <para>
/// <b>Regra ao acrescentar item sensível:</b> todo item marcado como sensível precisa de um
/// campo onde o opt-in seja gravado na concessão. Hoje existe exatamente um — o SCR, gravado
/// em <see cref="TenantCompartilhamentoEntity.IncluiScr"/>. Um segundo item sensível sem
/// coluna correspondente apareceria desmarcado na tela e seria compartilhado assim mesmo.
/// </para>
/// <para>
/// Alertas não estão na lista de propósito: são entregues apenas ao dono do tenant, então não
/// há o que consentir sobre eles.
/// </para>
/// </remarks>
public static class CatalogoDeCompartilhamento
{
    /// <summary>Único item sensível da fase 1. O app usa esta chave para preencher o opt-in.</summary>
    public const string ChaveScr = "scr";

    public static readonly IReadOnlyList<ItemCompartilhavel> Itens =
    [
        new("lancamentos", "Lançamentos",
            "Despesas e receitas com descrição, valor, data de competência e data de pagamento.",
            Sensivel: false),

        new("categorias", "Categorias",
            "Como cada lançamento está classificado.",
            Sensivel: false),

        new("orcamento", "Orçamento mensal",
            "Valores planejados por categoria em cada mês.",
            Sensivel: false),

        new("renda", "Renda declarada",
            "Fontes de renda cadastradas e os valores previstos.",
            Sensivel: false),

        new("relatorios", "Relatórios mensais",
            "Extrato do mês e resumo por categoria, com planejado, realizado e diferença.",
            Sensivel: false),

        new(ChaveScr, "Dívidas registradas no SCR",
            "Operações de crédito registradas no Banco Central, incluindo atraso e prejuízo.",
            Sensivel: true),
    ];
}
