using Microsoft.EntityFrameworkCore;

namespace NojectServer.Modules.Identity.Infrastructure.Persistence;

internal static class IdentityPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString =
            configuration.GetConnectionString("DBConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is not configured.");

        services.AddDbContext<IdentityDataContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        return services;
    }
}
