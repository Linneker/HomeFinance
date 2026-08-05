using System.Reflection;
using Acme.Sistemas.Core;
using Acme.Sistemas.HomeFinance.Api.Config;
using Acme.Sistemas.ExternalIntegration;
using Acme.Sistemas.Infrastructure;
using Acme.Sistemas.HomeFinance.Api.Middlewares;
using Acme.Sistemas.Infrastructure.Databases.Configuration;
using Acme.Sistemas.Repository;
using Acme.Sistemas.Services;
using Acme.Sistemas.Services.V1.Alertas;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservabilidade();
builder.AddAutenticacao();

// AddServices antes de AddCore: ela registra explicitamente o OnboardingBehavior, e o primeiro
// behavior registrado é o mais externo. A recusa por onboarding precisa acontecer antes do
// handler e antes dos behaviors que reavaliam alertas — avaliar um estado que não foi gravado
// emitiria aviso por uma operação que a pessoa não conseguiu concluir.
builder.Services.AddServices();

builder.Services.AddCore(
    Assembly.GetExecutingAssembly(),
    typeof(ServicesServiceCollecation).Assembly);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddExternalIntegration(builder.Configuration);
builder.Services.AddRepository();

// O contexto de isolamento é por requisição e alimenta as policies de RLS. Registrado pela
// classe concreta e exposto pela interface: o middleware precisa preenchê-lo, o resto do
// sistema só o lê.
builder.Services.AddScoped<ContextoRequisicaoHttp>();
builder.Services.AddScoped<IContextoRequisicao>(sp => sp.GetRequiredService<ContextoRequisicaoHttp>());

// A rotina diária assume um tenant de cada vez fora de requisição. Mesma instância do escopo:
// registrar outra faria a rotina escrever num contexto que os repositórios não leem.
builder.Services.AddScoped<IContextoAssumivel>(sp => sp.GetRequiredService<ContextoRequisicaoHttp>());

// O alerta de caixa depende do tempo passar: sem esta rotina, uma data que ficou descoberta
// durante a noite só seria avisada no próximo lançamento que a pessoa registrasse.
builder.Services.AddHostedService<RotinaDiariaDeAlertas>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Em container o TLS termina no ingress; redirecionar aqui só produz o aviso
// "Failed to determine the https port for redirect" a cada requisição.
var emContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

if (!emContainer)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();

// Depois de autenticar e antes de autorizar: a resolução do tenant depende das claims, e
// nenhum endpoint pode executar sem o contexto de isolamento estabelecido.
app.UseMiddleware<ResolucaoDeTenantMiddleware>();

app.UseAuthorization();

// Liveness: responde sem tocar em dependência externa. Se dependesse do cofre ou do banco,
// uma oscilação deles faria o Kubernetes reiniciar a aplicação em cascata.
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }))
   .AllowAnonymous()
   .WithName("HealthLive");

app.MapearEndpoints();

app.Run();

/// <summary>Exposta para o projeto de testes de integração poder subir a Api em memória.</summary>
public partial class Program;
