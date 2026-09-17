using NojectServer.Middlewares;
using NojectServer.OptionsSetup;

namespace NojectServer.DependencyInjection;

public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers the HTTP API, OpenAPI documentation, error handling, and CORS services.
    /// </summary>
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.ConfigureOptions<ApiBehaviorOptionsSetup>();

        // Register documentation services in every environment; Program limits their endpoints.
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.ConfigureOptions<ConfigureSwaggerGenOptions>();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddCors();
        services.ConfigureOptions<ConfigureCorsOptions>();

        return services;
    }
}
