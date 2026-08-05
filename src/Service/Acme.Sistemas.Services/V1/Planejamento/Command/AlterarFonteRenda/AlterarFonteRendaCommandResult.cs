using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.AlterarFonteRenda;

public sealed record AlterarFonteRendaCommandResult
{
    public bool Sucesso { get; init; }

    /// <summary>
    /// Identificador da vigência que passou a valer. Difere do informado quando a alteração
    /// abriu uma nova vigência — a tela precisa disso para não editar de novo a linha fechada.
    /// </summary>
    public Guid? FonteId { get; init; }

    /// <summary>
    /// Verdadeiro quando o histórico foi preservado com uma vigência nova; falso quando a
    /// própria linha foi corrigida por não haver mês fechado atrás dela.
    /// </summary>
    public bool AbriuNovaVigencia { get; init; }

    public DateOnly? VigenciaInicio { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static AlterarFonteRendaCommandResult Alterada(
        Guid fonteId,
        bool abriuNovaVigencia,
        DateOnly vigenciaInicio) =>
        new()
        {
            Sucesso = true,
            FonteId = fonteId,
            AbriuNovaVigencia = abriuNovaVigencia,
            VigenciaInicio = vigenciaInicio,
        };

    public static AlterarFonteRendaCommandResult Recusada(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };
}
