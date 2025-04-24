using Microsoft.Extensions.Options;
using NojectServer.Configurations;
using NojectServer.Middlewares;
using NojectServer.OptionsSetup;
using NojectServer.Repositories.Base;
using NojectServer.Repositories.Implementations;
using NojectServer.Repositories.Interfaces;
using NojectServer.Repositories.UnitOfWork;
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
using NojectServer.Services.Projects.Implementations;
using NojectServer.Services.Projects.Interfaces;

namespace NojectServer.DependencyInjection;

/// <summary>
/// Provides extension methods for IServiceCollection to register application services
/// and configure options for the Noject application.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application services with the dependency injection container.
    ///
    /// This method configures:
    /// - Authentication and authorization services
    /// - Email sending services
    /// - Password and token management services
    /// - Two-factor authentication services
    /// - Collaborator management services
    /// - Project access middleware
    ///
    /// Services are registered with appropriate lifetimes (Singleton, Scoped, or Transient)
    /// based on their state management requirements and usage patterns.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the services to</param>
    public static void AddServices(this IServiceCollection services)
    {
        // Register generic repository
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        // Register unit of work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        // Register specialized repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICollaboratorRepository, CollaboratorRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

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
        services.AddScoped<IProjectsService, ProjectsService>();

        // Register middlewares for verifying project ownership and access
        services.AddScoped<VerifyProjectOwnership>();
        services.AddScoped<VerifyProjectAccess>();
    }

    /// <summary>
    /// Configures application options from the application configuration.
    ///
    /// This method:
    /// - Sets up routing options (using lowercase URLs)
    /// - Configures email settings from the configuration
    /// - Configures JWT token settings from the configuration
    /// - Registers option validators to ensure configurations are valid at startup
    ///
    /// These options are used throughout the application to maintain consistent
    /// behavior and settings across different components.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the options to</param>
    /// <param name="configuration">The application configuration</param>
    public static void AddAppOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure application options from configuration
        services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<JwtTokenOptions>(configuration.GetSection("JWTSecrets"));

        // Register options validators
        services.AddSingleton<IValidateOptions<JwtTokenOptions>, JwtTokenOptionsValidator>();

        // Configure JWT bearer options
        services.ConfigureOptions<JwtBearerOptionsSetup>();
    }
}
