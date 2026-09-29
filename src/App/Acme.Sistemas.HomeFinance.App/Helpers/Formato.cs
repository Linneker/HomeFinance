using System.Globalization;
using Acme.Sistemas.HomeFinance.App.Models;

namespace Acme.Sistemas.HomeFinance.App.Helpers;

/// <summary>Formatação pt-BR de moeda e data, e rótulos de enums.</summary>
public static class Formato
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string Moeda(decimal valor) => valor.ToString("C", PtBr);

    public static string Data(DateOnly data) => data.ToString("dd/MM/yyyy", PtBr);

    public static string DataHora(DateTimeOffset quando) => quando.ToLocalTime().ToString("dd/MM/yyyy HH:mm", PtBr);

    public static string Mes(DateOnly competencia) => competencia.ToString("MMMM 'de' yyyy", PtBr);

    public static string Assinatura(SituacaoAssinatura situacao) => situacao switch
    {
        SituacaoAssinatura.Ativa => "Ativa",
        SituacaoAssinatura.Carencia => "Em carência",
        SituacaoAssinatura.Suspensa => "Suspensa",
        SituacaoAssinatura.Cancelada => "Cancelada",
        _ => "—",
    };

    public static string Exportacao(SituacaoExportacao situacao) => situacao switch
    {
        SituacaoExportacao.Solicitada => "Solicitada",
        SituacaoExportacao.Gerando => "Gerando",
        SituacaoExportacao.Concluida => "Concluída",
        SituacaoExportacao.Expirada => "Expirada",
        SituacaoExportacao.Cancelada => "Cancelada",
        SituacaoExportacao.Falhou => "Falhou",
        _ => "—",
    };

    public static string Alerta(TipoAlerta tipo) => tipo switch
    {
        TipoAlerta.Competencia => "Competência",
        TipoAlerta.Caixa => "Caixa",
        _ => "—",
    };
}
