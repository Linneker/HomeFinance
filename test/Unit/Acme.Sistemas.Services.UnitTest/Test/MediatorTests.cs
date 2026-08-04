using System.Reflection;
using Acme.Sistemas.Core;
using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Services.UnitTest.Fixture;
using Microsoft.Extensions.DependencyInjection;

namespace Acme.Sistemas.Services.UnitTest.Test;

public sealed class MediatorTests
{
    private static (IMediator Mediator, MediatorFixture.Rastro Rastro) Montar(
        Action<IServiceCollection>? extra = null)
    {
        var rastro = new MediatorFixture.Rastro();
        var services = new ServiceCollection();
        services.AddSingleton(rastro);
        extra?.Invoke(services);
        services.AddCore(Assembly.GetExecutingAssembly());

        var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<IMediator>(), rastro);
    }

    [Fact]
    public async Task SendAsync_deve_resolver_handler_e_devolver_resultado()
    {
        var (mediator, _) = Montar();

        var resultado = await mediator.SendAsync(new MediatorFixture.PingRequest("ok"));

        Assert.Equal("pong:ok", resultado);
    }

    [Fact]
    public async Task SendAsync_deve_executar_behaviors_aninhados_em_torno_do_handler()
    {
        var (mediator, rastro) = Montar();

        await mediator.SendAsync(new MediatorFixture.PingRequest("ok"));

        // O handler roda uma única vez, no centro: todo "antes" vem antes dele e todo
        // "depois" vem depois. Isso vale qualquer que seja a ordem entre os behaviors.
        var indiceHandler = rastro.Passos.IndexOf("handler");

        Assert.Single(rastro.Passos, passo => passo == "handler");
        Assert.All(
            rastro.Passos.Where(passo => passo.EndsWith(":antes")),
            passo => Assert.True(rastro.Passos.IndexOf(passo) < indiceHandler));
        Assert.All(
            rastro.Passos.Where(passo => passo.EndsWith(":depois")),
            passo => Assert.True(rastro.Passos.IndexOf(passo) > indiceHandler));
    }

    [Fact]
    public async Task Behavior_registrado_explicitamente_nao_deve_rodar_duas_vezes_com_a_varredura()
    {
        // Registro explícito (para controlar posição) somado à varredura do assembly não pode
        // duplicar o behavior — duplicado, ele executaria duas vezes por request.
        var (mediator, rastro) = Montar(services =>
            services.AddPipelineBehavior(typeof(MediatorFixture.BehaviorExterno)));

        await mediator.SendAsync(new MediatorFixture.PingRequest("ok"));

        Assert.Single(rastro.Passos, passo => passo == "externo:antes");
        Assert.Single(rastro.Passos, passo => passo == "externo:depois");
    }

    [Fact]
    public async Task SendAsync_deve_falhar_quando_nao_ha_handler_registrado()
    {
        var (mediator, _) = Montar();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.SendAsync(new MediatorFixture.RequestSemHandler("x")));
    }

    [Fact]
    public async Task SendAsync_deve_propagar_a_excecao_original_do_handler()
    {
        var (mediator, _) = Montar();

        var excecao = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.SendAsync(new MediatorFixture.FalhaRequest()));

        Assert.Equal("falha de dominio", excecao.Message);
    }

    [Fact]
    public async Task PublishAsync_deve_entregar_o_evento_a_todos_os_handlers()
    {
        var (mediator, rastro) = Montar();

        await mediator.PublishAsync(new MediatorFixture.AlgoAconteceu("valor"));

        Assert.Contains("ouvinte1:valor", rastro.Passos);
        Assert.Contains("ouvinte2:valor", rastro.Passos);
    }

    [Fact]
    public async Task PublishAsync_deve_propagar_a_excecao_original_do_ouvinte()
    {
        var (mediator, _) = Montar();

        // Sem desembrulhar TargetInvocationException, a exceção real sumiria do log.
        var excecao = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.PublishAsync(new MediatorFixture.EventoQueFalha()));

        Assert.Equal("falha no ouvinte", excecao.Message);
    }

    [Fact]
    public async Task PublishAsync_nao_deve_falhar_quando_nao_ha_ouvinte()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MediatorFixture.Rastro());
        services.AddCore();
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.PublishAsync(new MediatorFixture.AlgoAconteceu("sem-ouvinte"));
    }
}
