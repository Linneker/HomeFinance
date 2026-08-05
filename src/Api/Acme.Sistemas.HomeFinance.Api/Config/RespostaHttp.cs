using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.HomeFinance.Api.Config;

/// <summary>Traduz erros de domínio em resposta HTTP com o mesmo formato em todo endpoint.</summary>
public static class RespostaHttp
{
    /// <summary>
    /// Códigos que significam conflito com o estado atual, não entrada malformada. Sem esta
    /// distinção, "e-mail em uso" voltaria como 400 e o cliente não conseguiria diferenciar
    /// "corrija o que digitou" de "esta conta já existe".
    /// </summary>
    private static readonly HashSet<string> Conflitos =
    [
        "email_em_uso",
        "ja_compartilhado",
        "categoria_ja_existe",
        "recorrencia_ja_encerrada",

        // Editar ocorrência ainda prevista não é entrada malformada: é pedido válido contra
        // um estado que não aceita. A próxima materialização a regeraria por cima.
        "ocorrencia_prevista_nao_editavel",

        "fonte_renda_encerrada",

        // Cópia de orçamento parada à espera de aceite. É conflito com o estado do destino,
        // não erro de entrada: a mesma requisição, reenviada com a confirmação, passa.
        "confirmacao_necessaria",

        // Já existe assinatura viva na conta: conflito com o estado, não entrada malformada.
        "assinatura_ja_vigente",

        // A compra pertence a outra conta do aplicativo.
        "comprovante_em_outra_conta",
    ];

    /// <summary>
    /// O pedido é válido, mas o solicitante não pode fazê-lo naquele espaço. Vira 403 e não
    /// 400 para o app conseguir reagir mandando trocar de tenant, em vez de mandar corrigir
    /// um campo que está certo.
    /// </summary>
    private static readonly HashSet<string> Proibidos =
    [
        "repasse_nao_permitido",
        "revogacao_fora_do_proprio_tenant",

        // Onboarding e assinatura são 403 e não 400: o pedido está correto, quem não pode é a
        // conta. É o que permite ao app abrir a tela de assinatura ou a barra de progresso em
        // vez de pedir para corrigir um campo que está certo.
        "onboarding_pendente",
        "assinatura_inativa",
        "cota_do_plano_esgotada",
    ];

    /// <summary>
    /// Não existe o que operar. Fica fora daqui, de propósito, o
    /// <c>destinatario_nao_encontrado</c>: 404 por e-mail transformaria o endpoint de
    /// compartilhamento em consulta de existência de conta, testável em massa.
    /// </summary>
    private static readonly HashSet<string> NaoEncontrados =
    [
        "compartilhamento_inexistente",
        "tenant_nao_encontrado",
        "lancamento_nao_encontrado",
        "recorrencia_nao_encontrada",
        "regra_nao_encontrada",
        "fonte_renda_nao_encontrada",
    ];

    public static IResult Falha(IReadOnlyList<Error> erros)
    {
        var problema = new
        {
            erros = erros.Select(erro => new { erro.Codigo, erro.Mensagem, erro.Campo }),
        };

        if (erros.Any(erro => Proibidos.Contains(erro.Codigo)))
        {
            return Results.Json(problema, statusCode: StatusCodes.Status403Forbidden);
        }

        if (erros.Any(erro => NaoEncontrados.Contains(erro.Codigo)))
        {
            return Results.NotFound(problema);
        }

        return erros.Any(erro => Conflitos.Contains(erro.Codigo))
            ? Results.Conflict(problema)
            : Results.BadRequest(problema);
    }
}
