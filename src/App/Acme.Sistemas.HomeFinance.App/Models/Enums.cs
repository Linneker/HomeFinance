namespace Acme.Sistemas.HomeFinance.App.Models;

// Valores iguais aos do servidor (Acme.Sistemas.Domain.Enums). A Api serializa enum como número,
// então os valores precisam casar.

public enum SituacaoAssinatura
{
    Ativa = 1,
    Carencia = 2,
    Suspensa = 3,
    Cancelada = 4,
}

public enum CanalPagamento
{
    AppStore = 1,
    GooglePlay = 2,
    MercadoPago = 3,
    Asaas = 4,
}

public enum FormatoExportacao
{
    Csv = 1,
    Json = 2,
    Texto = 3,
}

public enum SituacaoExportacao
{
    Solicitada = 1,
    Gerando = 2,
    Concluida = 3,
    Expirada = 4,
    Cancelada = 5,
    Falhou = 6,
}

public enum TipoAlerta
{
    Competencia = 1,
    Caixa = 2,
}
