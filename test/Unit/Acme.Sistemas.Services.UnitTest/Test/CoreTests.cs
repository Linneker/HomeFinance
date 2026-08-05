using System.Net;
using System.Reflection;
using Acme.Sistemas.Core;
using Acme.Sistemas.Core.Mediators;
using Acme.Sistemas.Core.Mediators.Handler;
using Acme.Sistemas.Core.Response;
using Acme.Sistemas.Core.Response.Erros;
using Acme.Sistemas.Core.Security;
using Acme.Sistemas.Services;
using Acme.Sistemas.Services.V1.Classificacao;
using Microsoft.Extensions.DependencyInjection;

namespace Acme.Sistemas.Services.UnitTest.Test;

/// <summary>
/// Envelope de resposta, hash de senha, política de senha e registro de DI — a base que todas
/// as camadas usam e que, por isso, falha longe de onde o defeito estaria.
/// </summary>
public sealed class CoreTests
{
    // ---------------------------------------------------------------- envelope

    [Fact]
    public void Resposta_ok_carrega_conteudo_e_status_200()
    {
        var resposta = ResponseDefault<string>.Ok("pronto", "tudo certo");

        Assert.True(resposta.Sucesso);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("pronto", resposta.Conteudo);
        Assert.Equal("tudo certo", resposta.Mensagem);
        Assert.Empty(resposta.Erros);
    }

    [Fact]
    public void Resposta_de_criacao_usa_status_201()
    {
        var resposta = ResponseDefault<int>.Criado(7);

        Assert.True(resposta.Sucesso);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal(7, resposta.Conteudo);
        Assert.Null(resposta.Mensagem);
    }

    [Fact]
    public void Resposta_de_falha_preserva_o_status_pedido_e_os_erros()
    {
        var resposta = ResponseDefault<string>.Falha(
            HttpStatusCode.Conflict,
            "conflito",
            new Error("ja_existe", "Já existe."));

        Assert.False(resposta.Sucesso);
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Null(resposta.Conteudo);
        Assert.Single(resposta.Erros);
        Assert.Equal("ja_existe", resposta.Erros[0].Codigo);
    }

    // ---------------------------------------------------------------- hash

    [Fact]
    public void Senha_correta_confere_contra_o_proprio_hash()
    {
        var hash = Hash.GerarHashSenha("Senha!Forte1");

        Assert.True(Hash.SenhaConfere("Senha!Forte1", hash));
        Assert.False(Hash.SenhaConfere("Senha!Forte2", hash));
    }

    /// <summary>
    /// Sal por usuário: a mesma senha gera hashes diferentes. Sem isso, uma tabela arco-íris
    /// quebraria todas as contas que escolheram a mesma senha de uma vez só.
    /// </summary>
    [Fact]
    public void Mesma_senha_gera_hashes_diferentes()
    {
        var primeiro = Hash.GerarHashSenha("Senha!Forte1");
        var segundo = Hash.GerarHashSenha("Senha!Forte1");

        Assert.NotEqual(primeiro, segundo);
        Assert.True(Hash.SenhaConfere("Senha!Forte1", primeiro));
        Assert.True(Hash.SenhaConfere("Senha!Forte1", segundo));
    }

    /// <summary>O formato carrega as iterações para que aumentá-las não invalide o já gravado.</summary>
    [Fact]
    public void Hash_gravado_declara_algoritmo_e_iteracoes()
    {
        var partes = Hash.GerarHashSenha("Senha!Forte1").Split('$');

        Assert.Equal(4, partes.Length);
        Assert.Equal("pbkdf2-sha512", partes[0]);
        Assert.True(int.Parse(partes[1]) >= 100_000);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Gerar_hash_de_senha_vazia_e_erro_de_programacao(string? senha) =>
        Assert.ThrowsAny<ArgumentException>(() => Hash.GerarHashSenha(senha!));

    [Theory]
    [InlineData("", "pbkdf2-sha512$210000$c2Fs$Y2hhdmU=")]
    [InlineData("   ", "pbkdf2-sha512$210000$c2Fs$Y2hhdmU=")]
    [InlineData("Senha!Forte1", "")]
    [InlineData("Senha!Forte1", "   ")]
    public void Conferencia_com_entrada_vazia_e_falsa(string senha, string hash) =>
        Assert.False(Hash.SenhaConfere(senha, hash));

    /// <summary>
    /// Hash em formato desconhecido devolve falso, não exceção: um registro corrompido no banco
    /// não pode derrubar o login de toda a base.
    /// </summary>
    [Theory]
    [InlineData("sem-cifrao")]
    [InlineData("pbkdf2-sha512$210000$so-tres-partes")]
    [InlineData("outro-algoritmo$210000$c2Fs$Y2hhdmU=")]
    [InlineData("pbkdf2-sha512$nao-e-numero$c2Fs$Y2hhdmU=")]
    [InlineData("pbkdf2-sha512$210000$nao-e-base64!$Y2hhdmU=")]
    public void Hash_malformado_nao_confere_e_nao_estoura(string armazenado) =>
        Assert.False(Hash.SenhaConfere("Senha!Forte1", armazenado));

    // ---------------------------------------------------------------- política de senha

    [Fact]
    public void Senha_forte_nao_tem_criterio_pendente()
    {
        Assert.Empty(PasswordHelper.CriteriosNaoAtendidos("Senha!Forte1"));
        Assert.True(PasswordHelper.EhForte("Senha!Forte1"));
    }

    /// <summary>
    /// Senha ausente devolve um critério só. Listar os cinco de uma vez para quem não digitou
    /// nada seria ruído, não ajuda.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Senha_ausente_devolve_um_unico_criterio(string? senha)
    {
        var criterios = PasswordHelper.CriteriosNaoAtendidos(senha);

        Assert.Single(criterios);
        Assert.False(PasswordHelper.EhForte(senha));
    }

    [Theory]
    [InlineData("Ab1!x", "caracteres")]
    [InlineData("senha!forte1", "maiúscula")]
    [InlineData("SENHA!FORTE1", "minúscula")]
    [InlineData("SenhaForte!x", "número")]
    [InlineData("SenhaForte123", "especial")]
    public void Criterio_faltante_e_apontado_individualmente(string senha, string trecho)
    {
        var criterios = PasswordHelper.CriteriosNaoAtendidos(senha);

        Assert.Contains(criterios, criterio => criterio.Contains(trecho, StringComparison.Ordinal));
    }

    [Fact]
    public void Senha_curta_e_pobre_acumula_varios_criterios() =>
        Assert.True(PasswordHelper.CriteriosNaoAtendidos("ab").Count >= 3);

    // ---------------------------------------------------------------- registro de DI

    private sealed record Pergunta : IRequest<string>;

    private sealed class PerguntaHandler : IRequestHandler<Pergunta, string>
    {
        public Task<string> HandleAsync(Pergunta request, CancellationToken cancellationToken) =>
            Task.FromResult("resposta");
    }

    private sealed class BehaviorFechado : IPipelineBehavior<Pergunta, string>
    {
        public Task<string> HandleAsync(
            Pergunta request,
            RequestHandlerDelegate<string> next,
            CancellationToken cancellationToken) => next();
    }

    private sealed class BehaviorAberto<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
        where TRequest : IRequest<TResult>
    {
        public Task<TResult> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResult> next,
            CancellationToken cancellationToken) => next();
    }

    [Fact]
    public void Core_sem_assembly_registra_apenas_o_mediator()
    {
        var provedor = new ServiceCollection().AddCore().BuildServiceProvider();

        Assert.NotNull(provedor.GetService<IMediator>());
    }

    [Fact]
    public async Task Core_descobre_handler_no_assembly_informado()
    {
        var provedor = new ServiceCollection()
            .AddCore(Assembly.GetExecutingAssembly())
            .BuildServiceProvider();

        var mediator = provedor.GetRequiredService<IMediator>();

        Assert.Equal("resposta", await mediator.SendAsync(new Pergunta(), CancellationToken.None));
    }

    [Fact]
    public void Behavior_generico_aberto_vale_para_todo_request()
    {
        var provedor = new ServiceCollection()
            .AddPipelineBehavior(typeof(BehaviorAberto<,>))
            .AddCore(Assembly.GetExecutingAssembly())
            .BuildServiceProvider();

        Assert.NotEmpty(provedor.GetServices<IPipelineBehavior<Pergunta, string>>());
    }

    [Fact]
    public void Behavior_fechado_vale_para_o_request_que_ele_declara()
    {
        var provedor = new ServiceCollection()
            .AddPipelineBehavior(typeof(BehaviorFechado))
            .BuildServiceProvider();

        Assert.Contains(
            provedor.GetServices<IPipelineBehavior<Pergunta, string>>(),
            behavior => behavior is BehaviorFechado);
    }

    /// <summary>
    /// Registrar um tipo que não é behavior falha no arranque, e não silenciosamente: o
    /// pipeline seguiria rodando sem a etapa que alguém achou que tinha adicionado.
    /// </summary>
    [Fact]
    public void Tipo_que_nao_e_behavior_e_recusado_no_registro() =>
        Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AddPipelineBehavior(typeof(CoreTests)));

    [Fact]
    public void Registro_de_behavior_nulo_e_recusado() =>
        Assert.Throws<ArgumentNullException>(
            () => new ServiceCollection().AddPipelineBehavior(null!));

    /// <summary>
    /// O mesmo behavior registrado explicitamente e depois encontrado pela varredura não pode
    /// rodar duas vezes.
    /// </summary>
    [Fact]
    public void Behavior_registrado_duas_vezes_entra_uma_vez_so()
    {
        var provedor = new ServiceCollection()
            .AddPipelineBehavior(typeof(BehaviorFechado))
            .AddCore(Assembly.GetExecutingAssembly())
            .BuildServiceProvider();

        Assert.Single(
            provedor.GetServices<IPipelineBehavior<Pergunta, string>>(),
            behavior => behavior is BehaviorFechado);
    }

    /// <summary>
    /// Verifica o registro, e não a resolução: <c>ClassificacaoService</c> depende de um
    /// repositório que vive na camada de dados, ausente deste projeto de teste de propósito —
    /// resolver aqui exigiria referenciar a infraestrutura só para conferir uma linha de DI.
    /// </summary>
    [Fact]
    public void Camada_de_aplicacao_registra_relogio_e_classificacao()
    {
        var servicos = new ServiceCollection().AddServices();

        Assert.Contains(servicos, s => s.ServiceType == typeof(TimeProvider));
        Assert.Contains(
            servicos,
            s => s.ServiceType == typeof(IClassificacaoService)
                 && s.ImplementationType == typeof(ClassificacaoService));
    }

    /// <summary>
    /// Um relógio já registrado — o falso dos testes de integração — não pode ser substituído
    /// pelo do sistema quando a camada de aplicação entra.
    /// </summary>
    [Fact]
    public void Relogio_ja_registrado_nao_e_substituido()
    {
        var relogio = new FakeTimeProvider();

        var provedor = new ServiceCollection()
            .AddSingleton<TimeProvider>(relogio)
            .AddServices()
            .BuildServiceProvider();

        Assert.Same(relogio, provedor.GetRequiredService<TimeProvider>());
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);
    }
}
