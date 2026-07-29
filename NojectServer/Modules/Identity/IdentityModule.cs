using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using NojectServer.Modules.Identity.Application.Authentication.PasswordReset;
using NojectServer.Modules.Identity.Application.Email;
using NojectServer.Modules.Identity.Application.JwtTokens;
using NojectServer.Modules.Identity.Application.Passwords;
using NojectServer.Modules.Identity.Application.Persistence;
using NojectServer.Modules.Identity.Application.RefreshTokens;
using NojectServer.Modules.Identity.Application.Tokens;
using NojectServer.Modules.Identity.Application.TwoFactorAuthentication;
using NojectServer.Modules.Identity.Application.Users;
using NojectServer.Modules.Identity.Infrastructure.Email;
using NojectServer.Modules.Identity.Infrastructure.JwtTokens;
using NojectServer.Modules.Identity.Infrastructure.Passwords;
using NojectServer.Modules.Identity.Infrastructure.Persistence;
using NojectServer.Modules.Identity.Infrastructure.RefreshTokens;
using NojectServer.Modules.Identity.Infrastructure.Tokens;
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

        // Persistence services
        services.AddScoped<IIdentityTransaction, IdentityTransaction>();

        // Email services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // Password services
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        // Identity token services
        services.AddSingleton<IOpaqueTokenGenerator, OpaqueTokenGenerator>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // Two-factor authentication services
        services.AddScoped<ITwoFactorSecretProtector, TwoFactorSecretProtector>();
        services.AddSingleton<ITotpService, OtpNetTotpService>();
        services.AddScoped<ITwoFactorAuthService, TwoFactorAuthService>();

        // Authentication services
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IValidator<RequestPasswordResetInput>, RequestPasswordResetInputValidator>();
        services.AddSingleton<IValidator<ResetPasswordInput>, ResetPasswordInputValidator>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();

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
