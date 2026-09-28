namespace Acme.Sistemas.Domain.Enums;

/// <summary>
/// Ciclo de vida de uma solicitação de exportação.
/// </summary>
/// <remarks>
/// A geração é assíncrona: a solicitação nasce <see cref="Solicitada"/>, o trabalhador da fila a
/// leva a <see cref="Gerando"/> e depois a <see cref="Concluida"/>. O arquivo pronto vive por
/// prazo determinado e some ao expirar — <see cref="Expirada"/> é estado próprio, e não
/// concluída sem arquivo, para a tela poder dizer "o prazo acabou, peça de novo" em vez de um
/// erro genérico.
/// </remarks>
public enum SituacaoExportacao
{
    /// <summary>Registrada, aguardando o trabalhador da fila.</summary>
    Solicitada = 1,

    /// <summary>O trabalhador está montando o arquivo.</summary>
    Gerando = 2,

    /// <summary>Arquivo pronto e disponível para download.</summary>
    Concluida = 3,

    /// <summary>Prazo de disponibilidade encerrado; o arquivo foi removido do armazenamento.</summary>
    Expirada = 4,

    /// <summary>Cancelada pela desabilitação do recurso enquanto ainda não havia concluído.</summary>
    Cancelada = 5,

    /// <summary>A geração falhou. Registrada para o titular poder tentar de novo.</summary>
    Falhou = 6,
}
