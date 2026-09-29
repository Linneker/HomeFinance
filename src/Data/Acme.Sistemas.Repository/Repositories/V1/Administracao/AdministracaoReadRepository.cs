using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Administracao;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository.Helper;
using Acme.Sistemas.Repository.Repositories.V1.Administracao.Query;

namespace Acme.Sistemas.Repository.Repositories.V1.Administracao;

/// <inheritdoc cref="IAdministracaoReadRepository"/>
public sealed class AdministracaoReadRepository(TransactionManager transacoes) : IAdministracaoReadRepository
{
    public async Task<IReadOnlyList<ContaAdministrativa>> ListarContasAsync(
        CancellationToken cancellationToken)
    {
        await using var escopo = await transacoes.AbrirLeituraAsync(cancellationToken);
        await using var comando = new EscopoSql(escopo.Conexao, escopo.Transacao)
            .Comando(AdministracaoSql.ListarContas);

        var contas = new List<ContaAdministrativa>();

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);

        while (await leitor.ReadAsync(cancellationToken))
        {
            contas.Add(Ler(leitor));
        }

        return contas;
    }

    private static ContaAdministrativa Ler(DbDataReader leitor)
    {
        var situacaoOrdinal = leitor.GetOrdinal("assinatura_situacao");

        SituacaoAssinatura? situacao = leitor.IsDBNull(situacaoOrdinal)
            ? null
            : (SituacaoAssinatura)leitor.GetInt16(situacaoOrdinal);

        return new ContaAdministrativa(
            leitor.Identificador("usuario_id"),
            leitor.TextoObrigatorio("email"),
            leitor.DataHora("criado_em"),
            situacao,
            TextoOpcional(leitor, "plano_codigo"),
            TextoOpcional(leitor, "plano_nome"));
    }

    private static string? TextoOpcional(DbDataReader leitor, string coluna)
    {
        var ordinal = leitor.GetOrdinal(coluna);
        return leitor.IsDBNull(ordinal) ? null : leitor.GetString(ordinal);
    }
}
