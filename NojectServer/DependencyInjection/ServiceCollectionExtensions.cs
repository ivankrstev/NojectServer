using Microsoft.Extensions.Options;
using NojectServer.Configurations;
using NojectServer.Middlewares;
using NojectServer.Services.Auth.Implementations;
using NojectServer.Services.Auth.Interfaces;
using NojectServer.Services.Auth.Validation.Implementations;
using NojectServer.Services.Auth.Validation.Interfaces;
using NojectServer.Services.Collaborators.Implementations;
using NojectServer.Services.Collaborators.Interfaces;
using NojectServer.Services.Common.Implementations;
using NojectServer.Services.Common.Interfaces;
using NojectServer.Services.Email.Implementations;
using NojectServer.Services.Email.Interfaces;

namespace NojectServer.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection services)
    {
        // Register all services
        services.AddScoped<IAuthService, AuthService>();
        services.AddTransient<IEmailSender, SmtpEmailSender>();
        services.AddTransient<IEmailService, EmailService>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<ITwoFactorAuthService, TwoFactorAuthService>();
        services.AddScoped<ITotpValidator, OtpNetTotpValidator>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ICollaboratorsService, CollaboratorsService>();

        // Register middlewares for verifying project ownership and access
        services.AddScoped<VerifyProjectOwnership>();
        services.AddScoped<VerifyProjectAccess>();
    }

    public static void AddAppOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure application options from configuration
        services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<JwtTokenOptions>(configuration.GetSection("JWTSecrets"));

        // Register options validators
        services.AddSingleton<IValidateOptions<JwtTokenOptions>, JwtTokenOptionsValidator>();
    }
}
