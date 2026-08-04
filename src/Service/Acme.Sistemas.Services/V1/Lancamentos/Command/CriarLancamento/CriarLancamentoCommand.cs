using Acme.Sistemas.Core.Mediators.Handler;

namespace Acme.Sistemas.Services.V1.Lancamentos.Command.CriarLancamento;

/// <param name="DataCompetencia">
/// Quando o gasto aconteceu. Anulável de propósito: data ausente precisa virar recusa com a
/// indicação de qual data falta, e não um valor padrão silencioso.
/// </param>
/// <param name="DataCaixa">
/// Quando o dinheiro sai. Em compra parcelada, é o vencimento da primeira parcela.
/// </param>
/// <param name="Parcelas">1 é à vista. Acima disso, o valor é o total da compra.</param>
/// <param name="ConfirmouDuplicidade">
/// Resposta ao aviso de possível duplicidade. Falso na primeira tentativa; a segunda chamada,
/// já com a confirmação, grava.
/// </param>
public sealed record CriarLancamentoCommand(
    Guid TenantId,
    Guid UsuarioId,
    string Descricao,
    decimal Valor,
    DateOnly? DataCompetencia,
    DateOnly? DataCaixa,
    Guid? CategoriaId,
    int Parcelas = 1,
    bool ConfirmouDuplicidade = false) : IRequest<CriarLancamentoCommandResult>;
