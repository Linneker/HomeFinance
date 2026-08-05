using System.Globalization;
using Acme.Sistemas.Domain.Enums;

namespace Acme.Sistemas.Domain.Entities.Alertas;

/// <summary>
/// Aviso emitido ao dono do tenant sobre uma condição que ele precisa resolver.
/// </summary>
/// <remarks>
/// <see cref="ChaveCondicao"/> identifica a <em>condição</em>, não a ocorrência. É o que impede
/// o mesmo alerta de ser emitido de novo a cada lançamento gravado enquanto o quadro não muda —
/// e o que faz a dispensa continuar valendo entre sessões.
/// </remarks>
public sealed class AlertaEntity : TenantEntityBase
{
    public TipoAlerta Tipo { get; set; }

    public required string ChaveCondicao { get; set; }

    public EstadoAlerta Estado { get; set; } = EstadoAlerta.Aberto;

    public required string Mensagem { get; set; }

    public DateTimeOffset? EncerradoEm { get; set; }

    public DateTimeOffset? DispensadoEm { get; set; }

    /// <summary>
    /// A condição ainda vale — esteja o alerta visível ou dispensado.
    /// </summary>
    /// <remarks>
    /// É esta pergunta, e não "está aberto?", que decide se um novo alerta deve ser emitido.
    /// Emitir por cima de um dispensado desfaria em silêncio a escolha da pessoa.
    /// </remarks>
    public bool CondicaoAtiva => Estado is EstadoAlerta.Aberto or EstadoAlerta.Dispensado;

    public bool Visivel => Estado == EstadoAlerta.Aberto;

    public void Encerrar(DateTimeOffset quando)
    {
        Estado = EstadoAlerta.Encerrado;
        EncerradoEm = quando;
    }

    public void Dispensar(DateTimeOffset quando)
    {
        Estado = EstadoAlerta.Dispensado;
        DispensadoEm = quando;
    }
}

/// <summary>Monta as chaves de condição. Centralizado para os dois lados usarem a mesma forma.</summary>
/// <remarks>
/// Se a emissão e o encerramento montassem a chave cada um por sua conta, um alerta encerrado
/// por uma chave ligeiramente diferente ficaria aberto para sempre, sem nada na tela explicando
/// por quê.
/// </remarks>
public static class ChaveDeCondicao
{
    public static string OrcamentoEstourado(Guid categoriaId, DateOnly competencia) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"orcamento:{categoriaId:N}:{competencia:yyyy-MM}");

    /// <summary>
    /// A chave do caixa é a data da insuficiência, e não o mês.
    /// </summary>
    /// <remarks>
    /// Duas datas insuficientes no mesmo mês são dois problemas: pagar o dia 5 pode ser
    /// possível e o dia 20 não. Chavear por mês fundiria os dois e esconderia o segundo.
    /// </remarks>
    public static string SaldoInsuficiente(DateOnly data) =>
        string.Create(CultureInfo.InvariantCulture, $"caixa:{data:yyyy-MM-dd}");
}
