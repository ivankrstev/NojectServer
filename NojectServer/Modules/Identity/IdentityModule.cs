using Microsoft.AspNetCore.Authentication.JwtBearer;
using NojectServer.Modules.Identity.Infrastructure.Tokens;

namespace NojectServer.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddSingleton<JwtTokenValidationParametersFactory>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(
                JwtBearerDefaults.AuthenticationScheme,
                static _ => { });

        services
            .AddOptions<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtTokenValidationParametersFactory>(
                static (options, factory) =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters =
                        factory.CreateAccessTokenParameters();
                });

        services.AddAuthorization();

        return services;
    }
}
