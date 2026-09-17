using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using NojectServer.Shared.Application.Persistence;
using NojectServer.Shared.Infrastructure.Persistence;

namespace NojectServer.DependencyInjection;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString =
            configuration.GetConnectionString("DBConnection")
            ?? throw new InvalidOperationException(
                "Database connection string is not configured.");

        services.AddDbContext<DataContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
