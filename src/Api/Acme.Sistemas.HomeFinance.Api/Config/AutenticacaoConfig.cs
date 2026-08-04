using System.Text;
using Acme.Sistemas.Core.Security;
using Acme.Sistemas.Infrastructure.Secrets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Acme.Sistemas.HomeFinance.Api.Config;

public static class AutenticacaoConfig
{
    /// <summary>Chave de assinatura do JWT, lida do cofre.</summary>
    public const string ChaveSegredoJwt = "Jwt__Chave";

    public static IHostApplicationBuilder AddAutenticacao(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<JwtOptions>(
            builder.Configuration.GetSection(JwtOptions.SecaoConfiguracao));

        builder.Services.AddScoped<IProvedorDeChaveJwt, ProvedorDeChaveJwt>();
        builder.Services.AddScoped<IGeradorDeToken, GeradorDeToken>();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opcoes =>
            {
                opcoes.MapInboundClaims = false;

                opcoes.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    // Sem folga: o default de 5 minutos manteria token expirado válido por
                    // mais tempo do que a spec de sessão expirada admite.
                    ClockSkew = TimeSpan.Zero,
                };

                // A chave só existe em runtime, vinda do cofre — por isso é resolvida aqui e
                // não na configuração estática.
                opcoes.Events = new JwtBearerEvents
                {
                    OnMessageReceived = async contexto =>
                    {
                        if (opcoes.TokenValidationParameters.IssuerSigningKey is not null)
                        {
                            return;
                        }

                        var segredos = contexto.HttpContext.RequestServices
                            .GetRequiredService<ISecretProvider>();

                        var chave = await segredos.GetAsync(ChaveSegredoJwt, contexto.HttpContext.RequestAborted);

                        var opcoesJwt = contexto.HttpContext.RequestServices
                            .GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtOptions>>().Value;

                        opcoes.TokenValidationParameters.IssuerSigningKey =
                            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave));
                        opcoes.TokenValidationParameters.ValidIssuer = opcoesJwt.Emissor;
                        opcoes.TokenValidationParameters.ValidAudience = opcoesJwt.Audiencia;
                    },
                };
            });

        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();

        return builder;
    }
}
