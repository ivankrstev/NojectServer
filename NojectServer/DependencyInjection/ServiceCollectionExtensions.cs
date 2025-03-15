using NojectServer.Middlewares;
using NojectServer.Services.Auth.Implementations;
using NojectServer.Services.Auth.Interfaces;
using NojectServer.Services.Collaborators.Implementations;
using NojectServer.Services.Collaborators.Interfaces;
using NojectServer.Services.Common.Implementations;
using NojectServer.Services.Common.Interfaces;
using NojectServer.Services.Email;

namespace NojectServer.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection services)
    {
        // Register all services
        services.AddTransient<IEmailService, EmailService>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<ICollaboratorsService, CollaboratorsService>();

        // Register middlewares for verifying project ownership and access
        services.AddScoped<VerifyProjectOwnership>();
        services.AddScoped<VerifyProjectAccess>();
    }
}