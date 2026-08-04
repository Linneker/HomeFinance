namespace Acme.Sistemas.Domain.Entities.Users;

/// <summary>
/// Aceite de um documento legal. Guarda data, hora e versão porque a prova do consentimento é
/// exigência da LGPD, e "o usuário aceitou" sem versão não prova o que ele aceitou.
/// </summary>
public sealed class ConsentimentoEntity : BaseEntity
{
    public Guid UsuarioId { get; set; }

    public required string Documento { get; set; }

    public required string Versao { get; set; }

    public DateTimeOffset AceitoEm { get; set; }
}

/// <summary>Documentos cujo aceite é registrado.</summary>
public static class DocumentoConsentimento
{
    public const string TermosDeUso = "termos-de-uso";

    public const string PoliticaDePrivacidade = "politica-de-privacidade";

    public static readonly string[] Obrigatorios = [TermosDeUso, PoliticaDePrivacidade];
}
