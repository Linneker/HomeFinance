using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Domain.Entities.Administracao;

/// <summary>
/// A conta vista pelo back-office: identificação e situação, sem qualquer valor financeiro.
/// </summary>
/// <remarks>
/// O tipo carrega de propósito só o que a operação pode ver. Não há campo de lançamento, renda,
/// orçamento ou saldo — a fonte é uma função que não seleciona essas colunas, então não existe
/// caminho pelo qual um dado financeiro chegue até aqui.
/// </remarks>
/// <param name="Situacao">Situação da assinatura vigente, ou <c>null</c> quando a conta não tem assinatura viva.</param>
public sealed record ContaAdministrativa(
    Guid UsuarioId,
    string Email,
    DateTimeOffset CriadoEm,
    SituacaoAssinatura? Situacao,
    string? PlanoCodigo,
    string? PlanoNome);
