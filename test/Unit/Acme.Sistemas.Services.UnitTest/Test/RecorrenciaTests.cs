using Acme.Sistemas.Domain.Entities.Lancamentos;
using Acme.Sistemas.Domain.Interfaces.Repository;
using Acme.Sistemas.Services.V1.Lancamentos.Command.CriarRecorrencia;
using Acme.Sistemas.Services.V1.Lancamentos.Command.EncerrarRecorrencia;
using Moq;

namespace Acme.Sistemas.Services.UnitTest.Test;

public sealed class RecorrenciaTests
{
    private static readonly Guid Tenant = Guid.CreateVersion7();
    private static readonly Guid Ana = Guid.CreateVersion7();

    private sealed class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);
    }

    private static Mock<ICategoriaWriteRepository> Categorias()
    {
        var categorias = new Mock<ICategoriaWriteRepository>();
        categorias.Setup(r => r.ExisteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return categorias;
    }

    [Fact]
    public async Task Conta_fixa_materializa_o_horizonte_com_competencia_e_caixa_no_mesmo_dia()
    {
        var ocorrenciasGravadas = new List<LancamentoEntity>();
        var participacoesGravadas = new List<LancamentoParticipacaoEntity>();

        var recorrencias = new Mock<IRecorrenciaWriteRepository>();
        recorrencias.Setup(r => r.CriarAsync(
                It.IsAny<RecorrenciaEntity>(),
                It.IsAny<IReadOnlyList<LancamentoEntity>>(),
                It.IsAny<IReadOnlyList<LancamentoParticipacaoEntity>>(),
                It.IsAny<CancellationToken>()))
            .Callback<RecorrenciaEntity, IReadOnlyList<LancamentoEntity>,
                IReadOnlyList<LancamentoParticipacaoEntity>, CancellationToken>(
                (_, ocorrencias, participacoes, _) =>
                {
                    ocorrenciasGravadas.AddRange(ocorrencias);
                    participacoesGravadas.AddRange(participacoes);
                })
            .Returns(Task.CompletedTask);

        var handler = new CriarRecorrenciaCommandHandler(
            recorrencias.Object, Categorias().Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new CriarRecorrenciaCommand(
                Tenant, Ana, "Internet", 120m, DiaDoMes: 10, CategoriaId: null,
                Inicio: new DateOnly(2026, 8, 1)),
            CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(RecorrenciaEntity.MesesDeHorizonte, resultado.OcorrenciasGeradas);

        // Conta fixa consome e vence no mesmo dia: não há a defasagem da fatura de cartão.
        Assert.All(
            ocorrenciasGravadas,
            ocorrencia => Assert.Equal(ocorrencia.DataCompetencia, ocorrencia.DataCaixa));

        Assert.Equal(new DateOnly(2026, 8, 10), ocorrenciasGravadas[0].DataCaixa);
        Assert.Equal(new DateOnly(2027, 7, 10), ocorrenciasGravadas[^1].DataCaixa);

        // Previstas, não realizadas: entram na projeção de saldo, não no que já foi gasto.
        Assert.All(ocorrenciasGravadas, ocorrencia => Assert.True(ocorrencia.Previsto));

        // Uma participação por ocorrência: sem elas a projeção por pessoa ficaria vazia.
        Assert.Equal(ocorrenciasGravadas.Count, participacoesGravadas.Count);
    }

    [Fact]
    public async Task Dia_que_nao_existe_no_mes_recua_para_o_ultimo_dia_e_volta_depois()
    {
        var ocorrenciasGravadas = new List<LancamentoEntity>();

        var recorrencias = new Mock<IRecorrenciaWriteRepository>();
        recorrencias.Setup(r => r.CriarAsync(
                It.IsAny<RecorrenciaEntity>(),
                It.IsAny<IReadOnlyList<LancamentoEntity>>(),
                It.IsAny<IReadOnlyList<LancamentoParticipacaoEntity>>(),
                It.IsAny<CancellationToken>()))
            .Callback<RecorrenciaEntity, IReadOnlyList<LancamentoEntity>,
                IReadOnlyList<LancamentoParticipacaoEntity>, CancellationToken>(
                (_, ocorrencias, _, _) => ocorrenciasGravadas.AddRange(ocorrencias))
            .Returns(Task.CompletedTask);

        var handler = new CriarRecorrenciaCommandHandler(
            recorrencias.Object, Categorias().Object, new RelogioFixo());

        await handler.HandleAsync(
            new CriarRecorrenciaCommand(
                Tenant, Ana, "Aluguel", 1800m, DiaDoMes: 31, CategoriaId: null,
                Inicio: new DateOnly(2026, 12, 1)),
            CancellationToken.None);

        var datas = ocorrenciasGravadas.Select(o => o.DataCaixa).ToList();

        // Fevereiro recua para 28. Março tem que voltar ao 31 — se a série fosse somada mês a
        // mês a partir da anterior, ficaria presa no dia 28 até o fim do horizonte.
        Assert.Equal(new DateOnly(2026, 12, 31), datas[0]);
        Assert.Equal(new DateOnly(2027, 2, 28), datas[2]);
        Assert.Equal(new DateOnly(2027, 3, 31), datas[3]);
    }

    [Fact]
    public async Task Encerramento_corta_a_partir_de_hoje()
    {
        var recorrenciaId = Guid.CreateVersion7();

        var recorrencias = new Mock<IRecorrenciaWriteRepository>();
        recorrencias.Setup(r => r.ObterAsync(recorrenciaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecorrenciaEntity
            {
                Id = recorrenciaId,
                TenantId = Tenant,
                Descricao = "Internet",
                Valor = 120m,
                DiaDoMes = 10,
                Inicio = new DateOnly(2026, 1, 10),
                CriadoEm = DateTimeOffset.UtcNow,
            });
        recorrencias.Setup(r => r.EncerrarAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new EncerrarRecorrenciaCommandHandler(recorrencias.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new EncerrarRecorrenciaCommand(Tenant, recorrenciaId), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(new DateOnly(2026, 8, 4), resultado.APartirDe);

        // O corte é hoje: o que já passou permanece, inclusive ocorrência prevista que nunca
        // chegou a ser confirmada — apagá-la removeria do histórico um gasto que ocorreu.
        recorrencias.Verify(
            r => r.EncerrarAsync(
                recorrenciaId, new DateOnly(2026, 8, 4), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Encerrar_de_novo_nao_apaga_mais_nada()
    {
        var recorrenciaId = Guid.CreateVersion7();

        var recorrencias = new Mock<IRecorrenciaWriteRepository>();
        recorrencias.Setup(r => r.ObterAsync(recorrenciaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecorrenciaEntity
            {
                Id = recorrenciaId,
                TenantId = Tenant,
                Descricao = "Internet",
                Valor = 120m,
                DiaDoMes = 10,
                Inicio = new DateOnly(2026, 1, 10),
                EncerradaEm = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
                CriadoEm = DateTimeOffset.UtcNow,
            });
        recorrencias.Setup(r => r.EncerrarAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new EncerrarRecorrenciaCommandHandler(recorrencias.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new EncerrarRecorrenciaCommand(Tenant, recorrenciaId), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(EncerrarRecorrenciaCommandHandler.CodigoJaEncerrada, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Recorrencia_inexistente_e_recusada()
    {
        var recorrencias = new Mock<IRecorrenciaWriteRepository>();
        recorrencias.Setup(r => r.ObterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecorrenciaEntity?)null);

        var handler = new EncerrarRecorrenciaCommandHandler(recorrencias.Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new EncerrarRecorrenciaCommand(Tenant, Guid.CreateVersion7()), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            EncerrarRecorrenciaCommandHandler.CodigoRecorrenciaNaoEncontrada, resultado.Erros[0].Codigo);
    }

    [Fact]
    public async Task Dia_do_mes_fora_da_faixa_e_recusado()
    {
        var recorrencias = new Mock<IRecorrenciaWriteRepository>();
        var handler = new CriarRecorrenciaCommandHandler(
            recorrencias.Object, Categorias().Object, new RelogioFixo());

        var resultado = await handler.HandleAsync(
            new CriarRecorrenciaCommand(
                Tenant, Ana, "Internet", 120m, DiaDoMes: 32, CategoriaId: null,
                Inicio: new DateOnly(2026, 8, 1)),
            CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(
            CriarRecorrenciaCommandValidation.CodigoDiaInvalido, resultado.Erros[0].Codigo);
    }
}
