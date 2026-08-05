using Acme.Sistemas.Core.Response.Erros;

namespace Acme.Sistemas.Services.V1.Planejamento.Command.CadastrarFonteRenda;

public sealed record CadastrarFonteRendaCommandResult
{
    public bool Sucesso { get; init; }

    public Guid? FonteId { get; init; }

    /// <summary>Primeiro dia do mês em que a vigência começou, já normalizado.</summary>
    public DateOnly? VigenciaInicio { get; init; }

    /// <summary>Renda prevista do mês depois do cadastro, somando todas as fontes vigentes.</summary>
    public decimal RendaPrevista { get; init; }

    public IReadOnlyList<Error> Erros { get; init; } = [];

    public static CadastrarFonteRendaCommandResult Cadastrada(
        Guid fonteId,
        DateOnly vigenciaInicio,
        decimal rendaPrevista) =>
        new()
        {
            Sucesso = true,
            FonteId = fonteId,
            VigenciaInicio = vigenciaInicio,
            RendaPrevista = rendaPrevista,
        };

    public static CadastrarFonteRendaCommandResult Recusada(Error erro) =>
        new() { Sucesso = false, Erros = [erro] };
}
