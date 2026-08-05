namespace Acme.Sistemas.Domain.Entities.Assinaturas;

/// <summary>
/// Documento enviado enquanto a assinatura estava suspensa.
/// </summary>
/// <remarks>
/// Guardado e não processado. É a diferença entre suspender um serviço e reter o dado da pessoa:
/// o arquivo dela continua sendo dela, e o que fica indisponível é o trabalho que o produto faria
/// em cima dele.
/// </remarks>
public sealed class DocumentoRetidoEntity : TenantEntityBase
{
    /// <summary>Onde o arquivo está. O conteúdo vive no armazenamento de objetos.</summary>
    public required string Referencia { get; set; }

    public required string Tipo { get; set; }

    public DateTimeOffset? ProcessadoEm { get; set; }

    public bool Pendente => ProcessadoEm is null;
}
