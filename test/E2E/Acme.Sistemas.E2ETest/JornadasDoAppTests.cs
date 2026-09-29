namespace Acme.Sistemas.E2ETest;

/// <summary>
/// Jornadas E2E do app (13.19), a serem executadas com Appium contra um build do
/// <c>Acme.Sistemas.HomeFinance.App</c> em emulador/dispositivo.
/// </summary>
/// <remarks>
/// Ficam como <c>Skip</c> de propósito: Appium exige um device/emulador e um servidor Appium em
/// pé, que não existem no build headless de CI de unidade. Cada cenário abaixo descreve a jornada
/// que o harness deve dirigir; ao habilitar Appium (pacote <c>Appium.WebDriver</c> + um driver por
/// plataforma), remova o <c>Skip</c> e conduza os passos pela árvore de automação.
/// <para>
/// A regra de ouro do E2E aqui: dirigir pela UI real e afirmar contra o efeito observável na
/// própria UI — não contra o banco —, porque é a jornada da pessoa que está sob teste.
/// </para>
/// </remarks>
public sealed class JornadasDoAppTests
{
    private const string PendenteDeDispositivo =
        "E2E de app exige Appium com emulador/dispositivo; não roda no CI de unidade headless.";

    [Fact(Skip = PendenteDeDispositivo)]
    public void Criacao_de_conta_com_aceite_de_termos()
    {
        // Abrir o app → "Criar conta" → e-mail, senha, aceitar termos → cai no início autenticado.
    }

    [Fact(Skip = PendenteDeDispositivo)]
    public void Contratacao_de_plano_libera_recurso_apos_validacao_no_servidor()
    {
        // Início → Mais → Assinatura → Ver planos → contratar → recurso derivado passa a disponível.
    }

    [Fact(Skip = PendenteDeDispositivo)]
    public void Onboarding_progride_com_as_acoes_reais()
    {
        // Painel mostra a barra de onboarding; declarar renda e planejar orçamento sobem o percentual.
    }

    [Fact(Skip = PendenteDeDispositivo)]
    public void Primeiro_lancamento_com_duas_datas()
    {
        // Lançamentos → Novo → descrição, valor, competência e caixa → aparece no extrato do mês.
    }

    [Fact(Skip = PendenteDeDispositivo)]
    public void Classificacao_com_criacao_de_regra()
    {
        // Tocar um lançamento pendente → escolher categoria → "Criar regra" → some do pendente e a regra passa a existir.
    }

    [Fact(Skip = PendenteDeDispositivo)]
    public void Visualizacao_de_tenant_compartilhado_em_somente_leitura()
    {
        // Mais → Trocar espaço → escolher o espaço recebido → barra de somente leitura visível e ações de escrita bloqueadas.
    }
}
