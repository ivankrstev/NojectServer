using Microsoft.AspNetCore.Authentication.JwtBearer;
using NojectServer.Services.Auth.Interfaces;

namespace NojectServer.DependencyInjection;

/// <summary>
/// Extension methods for configuring JWT authentication in the application.
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// Adds JWT authentication and authorization services to the service collection.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    public static void AddJwtAuthentication(this IServiceCollection services)
    {
        // Register authentication with JWT Bearer
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(ConfigureJwtBearer);

        // Add authorization services
        services.AddAuthorization();
    }

    /// <summary>
    /// Configures JWT Bearer authentication options.
    /// This method handles token extraction for SignalR hubs and configures validation parameters.
    /// </summary>
    /// <param name="options">The JWT Bearer options to configure.</param>
    private static void ConfigureJwtBearer(JwtBearerOptions options)
    {
        // Configure JWT Bearer events to customize authentication behavior
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Extract the HTTP request path and query parameters
                var path = context.HttpContext.Request.Path;
                var accessToken = context.Request.Query["access_token"];

                // Special handling for SignalR hub connections which pass the token as a query parameter
                // This is necessary because SignalR WebSocket connections cannot send JWT tokens in the Authorization header
                if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/SharedProjectsHub") || path.StartsWithSegments("/TasksHub")))
                {
                    // Set the token from the query string so it can be validated
                    context.Token = accessToken;
                    return Task.CompletedTask;
                }

                // For non-SignalR requests, resolve the token validation service from DI
                // This approach avoids capturing scoped services during startup
                var tokenService = context.HttpContext.RequestServices.GetRequiredService<ITokenService>();

                // Configure token validation parameters dynamically at request time
                // This allows for runtime configuration of validation parameters
                options.TokenValidationParameters = tokenService.GetAccessTokenValidationParameters();

                return Task.CompletedTask;
            },
        };
    }
}
