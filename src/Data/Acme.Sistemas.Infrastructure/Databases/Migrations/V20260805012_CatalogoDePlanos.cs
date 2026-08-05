using Acme.Sistemas.Infrastructure.Databases.Migrations.Configuration;

namespace Acme.Sistemas.Infrastructure.Databases.Migrations;

/// <summary>
/// Catálogo inicial de planos.
/// </summary>
/// <remarks>
/// Separado da criação da tabela de propósito. A estrutura é código; o catálogo é decisão
/// comercial e muda por outro motivo, em outro ritmo. Juntar os dois faria uma mudança de preço
/// parecer uma alteração de schema no histórico.
/// <para>
/// Os identificadores são fixos para que a semeadura seja idempotente e para que os testes
/// possam se referir a um plano sem consultá-lo antes.
/// </para>
/// </remarks>
public sealed class V20260805012_CatalogoDePlanos : MigrationBase
{
    public override long Versao => 20260805012;

    public override string Descricao => "Catálogo inicial de planos com as cotas de compartilhamento";

    protected override string Sql => """
        INSERT INTO plano (id, codigo, nome, cota_compartilhamentos, preco_mensal, ativo, criado_em)
        VALUES
            -- Individual: a pessoa organiza a própria vida e não divide com ninguém.
            ('0199a000-0000-7000-8000-000000000001', 'individual', 'Individual',
             0, 19.90, true, now()),

            -- Casal: o caso que motivou o compartilhamento assimétrico.
            ('0199a000-0000-7000-8000-000000000002', 'casal', 'Casal',
             1, 29.90, true, now()),

            -- Família: pais e filhos adultos, cada um com o próprio espaço.
            ('0199a000-0000-7000-8000-000000000003', 'familia', 'Família',
             3, 39.90, true, now())
        ON CONFLICT (codigo) DO NOTHING;
        """;
}
