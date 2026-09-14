using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using NojectServer.Modules.Identity.Application.Authentication.EmailVerification;
using NojectServer.Modules.Identity.Application.Authentication.Login;
using NojectServer.Modules.Identity.Application.Authentication.PasswordReset;
using NojectServer.Modules.Identity.Application.Authentication.PendingEmailVerification;
using NojectServer.Modules.Identity.Application.Authentication.Register;
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
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<ITwoFactorAuthService, TwoFactorAuthService>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<IPendingEmailVerificationService, PendingEmailVerificationService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();

        // Configure authentication and authorization
        services.AddIdentityAuthentication();
        services.AddIdentityAuthorization();

        return services;
    }

    private static void AddIdentityValidators(
        this IServiceCollection services)
    {
        // Register
        services.AddTransient<IValidator<RegisterInput>, RegisterInputValidator>();

        // Login
        services.AddTransient<IValidator<LoginInput>, LoginInputValidator>();
        services.AddTransient<IValidator<CompleteTwoFactorLoginInput>, CompleteTwoFactorLoginInputValidator>();

        // Email verification
        services.AddTransient<IValidator<VerifyEmailInput>, VerifyEmailInputValidator>();
        services.AddTransient<IValidator<RequestEmailVerificationInput>, RequestEmailVerificationInputValidator>();

        // Pending email verification
        services.AddTransient<IValidator<ChangePendingEmailInput>, ChangePendingEmailInputValidator>();
        services.AddTransient<IValidator<DeletePendingAccountInput>, DeletePendingAccountInputValidator>();

        // Password reset
        services.AddTransient<IValidator<RequestPasswordResetInput>, RequestPasswordResetInputValidator>();
        services.AddTransient<IValidator<ResetPasswordInput>, ResetPasswordInputValidator>();
    }

    private static void AddIdentityAuthentication(
        this IServiceCollection services)
    {
        // Use access tokens as the default authentication scheme.
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(
                JwtBearerDefaults.AuthenticationScheme,
                static _ => { })
            .AddJwtBearer(
                IdentitySecurity.PendingEmailVerificationAuthenticationScheme,
                static _ => { });

        // Validate regular access tokens with the access-token signing key.
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

        // Validate pending-verification tokens with their dedicated signing key.
        services
            .AddOptions<JwtBearerOptions>(
                IdentitySecurity.PendingEmailVerificationAuthenticationScheme)
            .Configure<JwtTokenValidationParametersFactory>(
                static (options, factory) =>
                {
                    options.MapInboundClaims = false;

                    options.TokenValidationParameters =
                        factory.CreatePendingEmailVerificationTokenParameters();
                });
    }

    private static void AddIdentityAuthorization(
        this IServiceCollection services)
    {
        // Restrict pending-account actions to authenticated pending-verification tokens.
        services
            .AddAuthorizationBuilder()
            .AddPolicy(
                IdentitySecurity.PendingEmailVerificationPolicy,
                policy =>
                {
                    policy.AddAuthenticationSchemes(
                        IdentitySecurity.PendingEmailVerificationAuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(
                        IdentityTokenConstants.TokenPurposeClaimType,
                        IdentityTokenConstants.PendingEmailVerificationTokenPurpose);
                });
    }
}
