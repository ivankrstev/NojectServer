using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using NojectServer.Modules.Identity.Application.JwtTokens;
using NojectServer.Modules.Identity.Application.RefreshTokens;
using NojectServer.Modules.Identity.Application.TwoFactorAuthentication;
using NojectServer.Modules.Identity.Infrastructure.JwtTokens;
using NojectServer.Modules.Identity.Infrastructure.RefreshTokens;
using NojectServer.Modules.Identity.Infrastructure.TwoFactorAuthentication;

namespace NojectServer.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        // Shared infrastructure services
        services.AddSingleton(TimeProvider.System);
        services
            .AddDataProtection()
            .SetApplicationName("NojectServer.Identity");
        services.AddSingleton<JwtTokenValidationParametersFactory>();

        // Identity token services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // Two-factor authentication services
        services.AddScoped<ITwoFactorSecretProtector, TwoFactorSecretProtector>();

        // Configure authentication and authorization
        services.AddAuthenticationConfiguration();
        services.AddAuthorization();

        return services;
    }

    private static void AddAuthenticationConfiguration(
        this IServiceCollection services)
    {
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
    }
}
