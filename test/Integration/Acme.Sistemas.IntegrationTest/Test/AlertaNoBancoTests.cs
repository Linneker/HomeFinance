using System.Data.Common;
using Acme.Sistemas.Domain.Entities.Alertas;
using Acme.Sistemas.Domain.Entities.Tenants;
using Acme.Sistemas.Domain.Entities.Users;
using Acme.Sistemas.Domain.Enums;
using Acme.Sistemas.IntegrationTest.Config;
using Acme.Sistemas.Repository.Repositories.V1.Alerta;
using Acme.Sistemas.Repository.Repositories.V1.Tenant;
using Acme.Sistemas.Repository.Repositories.V1.Usuario;
using Acme.Sistemas.Services.V1.Alertas.Command.DispensarAlerta;
using Acme.Sistemas.Services.V1.Alertas.Query.AlertasAbertos;
using Acme.Sistemas.Services.V1.Assinaturas;
using Acme.Sistemas.Services.V1.Compartilhamento.Command.CompartilharTenant;
using Moq;

namespace Acme.Sistemas.IntegrationTest.Test;

/// <summary>
/// O que sustenta o ciclo de vida do alerta fora do serviço: o índice parcial que impede a
/// duplicidade e a entrega restrita ao dono.
/// </summary>
/// <remarks>
/// Os testes unitários provam a decisão de emitir, encerrar e dispensar contra um repositório em
/// memória que <em>declara</em> espelhar o índice <c>ux_alerta_condicao_aberta</c>. Essa
/// declaração é a parte não verificada: o <c>INSERT</c> real termina em
/// <c>ON CONFLICT DO NOTHING</c>, que descarta a gravação em silêncio. Se o índice não existisse,
/// ou não fosse parcial, nada falharia — o alerta simplesmente pararia de aparecer, e apenas em
/// produção.
/// </remarks>
[Collection(nameof(BancoCollection))]
public sealed class AlertaNoBancoTests(AmbienteBanco ambiente)
{
    private static CancellationToken Ct => CancellationToken.None;

    private sealed record Conta(Guid UsuarioId, Guid TenantId, string Email);

    private static IAssinaturaService ComCota()
    {
        var assinaturas = new Mock<IAssinaturaService>();

        assinaturas.Setup(a => a.SituacaoAtualAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SituacaoDaConta(SituacaoAssinatura.Ativa, "casal", 3, 0));

        return assinaturas.Object;
    }

    private async Task<Conta> CriarContaAsync(ContextoFixo contexto)
    {
        var usuarioId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var email = $"{usuarioId:N}@teste.local";
        var agora = DateTimeOffset.UtcNow;

        contexto.UsuarioId = null;
        contexto.TenantId = null;
        contexto.SomenteLeitura = false;

        await new UsuarioWriteRepository(ambiente.CriarGerenciador(contexto)).CriarContaAsync(
            new UsuarioEntity { Id = usuarioId, Email = email, SenhaHash = "hash", CriadoEm = agora },
            new TenantEntity { Id = tenantId, UsuarioId = usuarioId, CriadoEm = agora },
            [.. DocumentoConsentimento.Obrigatorios.Select(documento => new ConsentimentoEntity
            {
                Id = Guid.CreateVersion7(),
                UsuarioId = usuarioId,
                Documento = documento,
                Versao = "1.0",
                AceitoEm = agora,
            })],
            Ct);

        return new Conta(usuarioId, tenantId, email);
    }

    private static void Ativar(ContextoFixo contexto, Conta conta, bool somenteLeitura)
    {
        contexto.UsuarioId = conta.UsuarioId;
        contexto.TenantId = conta.TenantId;
        contexto.SomenteLeitura = somenteLeitura;
    }

    /// <summary>Concede o acesso pelo Command real e deixa o contexto como o middleware deixaria.</summary>
    private async Task CompartilharAsync(ContextoFixo contexto, Conta dono, Conta destinatario)
    {
        Ativar(contexto, dono, somenteLeitura: false);

        var concessao = await new CompartilharTenantCommandHandler(
            new TenantWriteRepository(ambiente.CriarGerenciador(contexto)),
            ComCota(),
            TimeProvider.System)
            .HandleAsync(
                new CompartilharTenantCommand(
                    dono.UsuarioId, dono.TenantId, destinatario.Email,
                    ConsentimentoConfirmado: true, IncluiScr: false),
                Ct);

        Assert.True(concessao.Sucesso);

        contexto.UsuarioId = destinatario.UsuarioId;
        contexto.TenantId = null;
        contexto.SomenteLeitura = true;

        var acesso = await new TenantWriteRepository(ambiente.CriarGerenciador(contexto))
            .ResolverAcessoAsync(destinatario.UsuarioId, dono.TenantId, Ct);

        Assert.NotNull(acesso);
        Assert.False(acesso.EhProprio);
        Assert.True(acesso.SomenteLeitura);

        contexto.UsuarioId = destinatario.UsuarioId;
        contexto.TenantId = acesso.TenantId;
        contexto.SomenteLeitura = acesso.SomenteLeitura;
    }

    private static AlertaEntity Alerta(
        Conta dono, string chave, string mensagem, TipoAlerta tipo = TipoAlerta.Competencia) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = dono.TenantId,
        Tipo = tipo,
        ChaveCondicao = chave,
        Estado = EstadoAlerta.Aberto,
        Mensagem = mensagem,
        CriadoEm = DateTimeOffset.UtcNow,
    };

    // ---------------------------------------------------------------- duplicidade

    /// <summary>
    /// "não emite um novo alerta para a mesma condição" — aqui provado contra o índice, e não
    /// contra a verificação em memória do serviço.
    /// </summary>
    /// <remarks>
    /// Esta é a barreira que cobre a corrida: duas avaliações simultâneas leem a tabela antes de
    /// qualquer uma gravar, ambas concluem que o alerta é novo, e só o índice separa as duas.
    /// </remarks>
    [Fact]
    public async Task Segunda_emissao_da_mesma_condicao_aberta_nao_cria_linha_nova()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);

        Ativar(contexto, ana, somenteLeitura: false);

        var alertas = new AlertaWriteRepository(ambiente.CriarGerenciador(contexto));

        var primeiro = Alerta(ana, "orcamento:transporte:2026-08", "Transporte passou do orçamento.");

        await alertas.GravarAsync(primeiro, Ct);
        await alertas.GravarAsync(
            Alerta(ana, "orcamento:transporte:2026-08", "Transporte passou de novo."), Ct);

        var abertos = await new AlertaReadRepository(ambiente.CriarGerenciador(contexto))
            .ListarAbertosAsync(Ct);

        var unico = Assert.Single(abertos);

        Assert.Equal(primeiro.Id, unico.Id);
        Assert.Equal("Transporte passou do orçamento.", unico.Mensagem);
    }

    /// <summary>
    /// "só volta a ser emitido se a condição for encerrada e ocorrer novamente": o que permite a
    /// volta é o <c>WHERE estado = 1</c> do índice.
    /// </summary>
    /// <remarks>
    /// Sem a cláusula parcial, o índice cobriria também o encerrado e o
    /// <c>ON CONFLICT DO NOTHING</c> engoliria esta segunda emissão sem erro. O usuário nunca
    /// mais seria avisado daquela categoria, e nenhum teste de serviço acusaria.
    /// </remarks>
    [Fact]
    public async Task Condicao_encerrada_libera_nova_emissao_da_mesma_chave()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);

        Ativar(contexto, ana, somenteLeitura: false);

        var alertas = new AlertaWriteRepository(ambiente.CriarGerenciador(contexto));

        var primeiro = Alerta(ana, "orcamento:lazer:2026-08", "Lazer passou do orçamento.");

        await alertas.GravarAsync(primeiro, Ct);

        primeiro.Encerrar(DateTimeOffset.UtcNow);

        Assert.True(await alertas.AtualizarEstadoAsync(primeiro, Ct));

        var segundo = Alerta(ana, "orcamento:lazer:2026-08", "Lazer passou do orçamento de novo.");

        await alertas.GravarAsync(segundo, Ct);

        var abertos = await new AlertaReadRepository(ambiente.CriarGerenciador(contexto))
            .ListarAbertosAsync(Ct);

        var unico = Assert.Single(abertos);

        Assert.Equal(segundo.Id, unico.Id);
    }

    /// <summary>
    /// O dispensado tem estado 3 e fica fora do índice parcial: no banco, a chave está livre.
    /// Quem impede a reemissão é o serviço, que trata aberto e dispensado como condição ativa.
    /// </summary>
    /// <remarks>
    /// Fica registrado porque a divisão é fácil de perder de vista: remover a verificação do
    /// serviço confiando "no índice" reabriria o aviso que a pessoa acabou de dispensar, e o
    /// banco aceitaria calado.
    /// </remarks>
    [Fact]
    public async Task Indice_nao_alcanca_o_dispensado_quem_segura_a_reemissao_e_o_servico()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);

        Ativar(contexto, ana, somenteLeitura: false);

        var alertas = new AlertaWriteRepository(ambiente.CriarGerenciador(contexto));

        var alerta = Alerta(
            ana, "caixa:2026-08-10", "Saldo projetado insuficiente.", TipoAlerta.Caixa);

        await alertas.GravarAsync(alerta, Ct);

        var dispensa = await new DispensarAlertaCommandHandler(alertas, TimeProvider.System)
            .HandleAsync(new DispensarAlertaCommand(ana.TenantId, alerta.Id), Ct);

        Assert.True(dispensa.Sucesso);

        // A condição segue ativa para o serviço, mesmo com o alerta fora da tela.
        var ativos = await alertas.ListarPorCondicaoAtivaAsync(TipoAlerta.Caixa, Ct);

        var ativo = Assert.Single(ativos);

        Assert.Equal(EstadoAlerta.Dispensado, ativo.Estado);
        Assert.False(ativo.Visivel);
        Assert.True(ativo.CondicaoAtiva);

        // E o alerta dispensado não volta para a lista da tela.
        Assert.Empty(await new AlertaReadRepository(ambiente.CriarGerenciador(contexto))
            .ListarAbertosAsync(Ct));
    }

    // ---------------------------------------------------------------- entrega

    /// <summary>
    /// Cenário literal da spec: o alerta gerado no tenant da Ana é entregue à Ana, e o Bruno,
    /// com acesso somente leitura ao mesmo tenant, não o recebe.
    /// </summary>
    /// <remarks>
    /// A segunda metade é o ponto: a RLS <em>deixa</em> o Bruno ler a linha, porque a policy de
    /// SELECT vale para o tenant compartilhado. O teste confirma isso indo ao repositório
    /// direto — e mostra, logo depois, que a consulta devolve vazio mesmo assim. A recusa é
    /// decisão do produto, e não efeito colateral do isolamento; sem esta prova, retirar a
    /// verificação do handler pareceria seguro.
    /// </remarks>
    [Fact]
    public async Task Alerta_do_dono_chega_ao_dono_e_nao_a_quem_tem_somente_leitura()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);
        var bruno = await CriarContaAsync(contexto);

        Ativar(contexto, ana, somenteLeitura: false);

        await new AlertaWriteRepository(ambiente.CriarGerenciador(contexto)).GravarAsync(
            Alerta(ana, "orcamento:mercado:2026-08", "Mercado passou do orçamento."), Ct);

        var daAna = await new AlertasAbertosQueryHandler(
                new AlertaReadRepository(ambiente.CriarGerenciador(contexto)))
            .HandleAsync(new AlertasAbertosQuery(ana.TenantId, SomenteLeitura: false), Ct);

        var entregue = Assert.Single(daAna.Alertas);

        Assert.Equal("Mercado passou do orçamento.", entregue.Mensagem);

        await CompartilharAsync(contexto, ana, bruno);

        var gerenciador = ambiente.CriarGerenciador(contexto);

        // A linha está ao alcance do Bruno no banco...
        Assert.Single(await new AlertaReadRepository(gerenciador).ListarAbertosAsync(Ct));

        // ...e ainda assim ele não recebe alerta nenhum.
        var doBruno = await new AlertasAbertosQueryHandler(new AlertaReadRepository(gerenciador))
            .HandleAsync(new AlertasAbertosQuery(ana.TenantId, SomenteLeitura: true), Ct);

        Assert.Empty(doBruno.Alertas);
    }

    /// <summary>
    /// Emitir e dispensar são escrita. No tenant recebido por compartilhamento, quem recusa é o
    /// banco — e recusa de duas formas diferentes.
    /// </summary>
    /// <remarks>
    /// O <c>INSERT</c> estoura na policy de <c>WITH CHECK</c>; o <c>UPDATE</c> não estoura, a
    /// policy filtra a linha e a operação afeta zero registros. O Command traduz esse zero em
    /// "alerta não encontrado", que é o que o convidado precisa ver — e não um erro de banco.
    /// </remarks>
    [Fact]
    public async Task Destinatario_somente_leitura_nao_emite_nem_dispensa_alerta_do_dono()
    {
        var contexto = new ContextoFixo();

        var ana = await CriarContaAsync(contexto);
        var bruno = await CriarContaAsync(contexto);

        Ativar(contexto, ana, somenteLeitura: false);

        var daAna = Alerta(ana, "orcamento:farmacia:2026-08", "Farmácia passou do orçamento.");

        await new AlertaWriteRepository(ambiente.CriarGerenciador(contexto)).GravarAsync(daAna, Ct);

        await CompartilharAsync(contexto, ana, bruno);

        var alertas = new AlertaWriteRepository(ambiente.CriarGerenciador(contexto));

        await Assert.ThrowsAnyAsync<DbException>(async () =>
            await alertas.GravarAsync(
                Alerta(ana, "orcamento:inventado:2026-08", "Alerta forjado pelo Bruno."), Ct));

        // Ler pode: o alerta da Ana aparece para quem recebeu o compartilhamento.
        Assert.NotNull(await alertas.ObterAsync(daAna.Id, Ct));

        var dispensa = await new DispensarAlertaCommandHandler(alertas, TimeProvider.System)
            .HandleAsync(new DispensarAlertaCommand(ana.TenantId, daAna.Id), Ct);

        Assert.False(dispensa.Sucesso);
        Assert.Equal(
            DispensarAlertaCommandHandler.CodigoAlertaNaoEncontrado, dispensa.Erros[0].Codigo);

        // O alerta da Ana continua aberto para ela.
        Ativar(contexto, ana, somenteLeitura: false);

        var abertos = await new AlertaReadRepository(ambiente.CriarGerenciador(contexto))
            .ListarAbertosAsync(Ct);

        var unico = Assert.Single(abertos);

        Assert.Equal(daAna.Id, unico.Id);
        Assert.Equal(EstadoAlerta.Aberto, unico.Estado);
    }
}
