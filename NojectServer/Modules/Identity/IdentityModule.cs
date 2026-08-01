using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using NojectServer.Modules.Identity.Application.Authentication.EmailVerification;
using NojectServer.Modules.Identity.Application.Authentication.Login;
using NojectServer.Modules.Identity.Application.Authentication.PasswordReset;
using NojectServer.Modules.Identity.Application.Authentication.TwoFactorAuthentication;
using NojectServer.Modules.Identity.Application.Email;
using NojectServer.Modules.Identity.Application.JwtTokens;
using NojectServer.Modules.Identity.Application.Passwords;
using NojectServer.Modules.Identity.Application.Persistence;
using NojectServer.Modules.Identity.Application.RefreshTokens;
using NojectServer.Modules.Identity.Application.Tokens;
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

        // Persistence
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IIdentityTransaction, IdentityTransaction>();

        // Email infrastructure
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IEmailService, EmailService>();

        // Password infrastructure
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        // Token infrastructure
        services.AddSingleton<IOpaqueTokenGenerator, OpaqueTokenGenerator>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // Two-factor authentication infrastructure
        services.AddScoped<ITwoFactorSecretProtector, TwoFactorSecretProtector>();
        services.AddSingleton<ITotpService, OtpNetTotpService>();

        // Validators
        services.AddIdentityValidators();

        // Application services
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<ITwoFactorAuthService, TwoFactorAuthService>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();

        // Configure authentication and authorization
        services.AddJwtAuthentication();
        services.AddAuthorization();

        return services;
    }

    private static void AddIdentityValidators(
        this IServiceCollection services)
    {
        // Login
        services.AddTransient<IValidator<LoginInput>, LoginInputValidator>();
        services.AddTransient<IValidator<CompleteTwoFactorLoginInput>, CompleteTwoFactorLoginInputValidator>();

        // Email verification
        services.AddTransient<IValidator<VerifyEmailInput>, VerifyEmailInputValidator>();
        services.AddTransient<IValidator<RequestEmailVerificationInput>, RequestEmailVerificationInputValidator>();

        // Password reset
        services.AddTransient<IValidator<RequestPasswordResetInput>, RequestPasswordResetInputValidator>();
        services.AddTransient<IValidator<ResetPasswordInput>, ResetPasswordInputValidator>();
    }

    private static void AddJwtAuthentication(
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
