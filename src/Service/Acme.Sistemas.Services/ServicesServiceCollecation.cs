using Acme.Sistemas.Services.V1.Alertas;
using Acme.Sistemas.Services.V1.Assinaturas;
using Acme.Sistemas.Services.V1.Classificacao;
using Acme.Sistemas.Services.V1.Exportacao;
using Acme.Sistemas.Services.V1.Exportacao.Formatadores;
using Acme.Sistemas.Services.V1.Onboarding;
using Acme.Sistemas.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Acme.Sistemas.Services;

/// <summary>
/// DI da camada de aplicação. A grafia do nome é herdada do padrão da plataforma e deve ser
/// mantida para não divergir dos demais projetos Acme.
/// </summary>
public static class ServicesServiceCollecation
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.TryAddTimeProvider();

        // Serviço de domínio compartilhado entre Commands: a classificação roda ao gravar
        // lançamento e ao corrigir a categoria à mão.
        services.AddScoped<IClassificacaoService, ClassificacaoService>();

        // Chamado pelos behaviors de lançamento e pela rotina diária de caixa.
        services.AddScoped<IAlertaService, AlertaService>();
        services.AddScoped<RotinaDeAlertasDeCaixa>();

        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IAssinaturaService, AssinaturaService>();
        services.AddScoped<RotinaDeReconciliacao>();
        services.AddScoped<FilaDeDocumentosRetidos>();

        // Exportação de dados. Um formatador por formato; o resolvedor os recebe por injeção e
        // recusa o que não estiver registrado. O gerador é o trabalhador da fila; a rotina de
        // expiração limpa os arquivos vencidos. O aviso é o seam de entrega ao titular.
        services.AddScoped<IFormatadorExportacao, FormatadorCsv>();
        services.AddScoped<IFormatadorExportacao, FormatadorJson>();
        services.AddScoped<IFormatadorExportacao, FormatadorTexto>();
        services.AddScoped<IResolvedorDeFormato, ResolvedorDeFormato>();
        services.AddScoped<IAvisoTitular, AvisoTitularPorLog>();
        services.AddScoped<GeradorDeExportacao>();
        services.AddScoped<RotinaDeExpiracao>();

        // Registrado antes da varredura de assembly: o primeiro behavior registrado é o mais
        // externo, e a recusa por onboarding precisa acontecer antes do handler e antes dos
        // behaviors que reavaliam alertas — avaliar um estado que não foi gravado emitiria
        // aviso por uma operação que a pessoa não conseguiu concluir.
        services.AddPipelineBehavior(typeof(OnboardingBehavior<,>));

        // Depois do onboarding, e por isso mais interno: com onboarding pendente e assinatura
        // suspensa, tentar lançar responde sobre o onboarding — que é o que de fato barra
        // aquele pedido — e não sobre a assinatura.
        services.AddPipelineBehavior(typeof(AssinaturaBehavior<,>));

        // Handlers e behaviors são descobertos por AddCore(assembly) no Program.cs.
        return services;
    }

    private static void TryAddTimeProvider(this IServiceCollection services)
    {
        // Injetar o relógio em vez de chamar DateTimeOffset.UtcNow direto é o que torna
        // testável tudo que depende de data — competência, caixa, vigência de renda.
        if (services.All(descriptor => descriptor.ServiceType != typeof(TimeProvider)))
        {
            services.AddSingleton(TimeProvider.System);
        }
    }
}
