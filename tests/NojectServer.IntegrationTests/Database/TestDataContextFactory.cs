using Microsoft.EntityFrameworkCore;
using NojectServer.Modules.Identity.Infrastructure.Persistence;

namespace NojectServer.IntegrationTests.Database;

internal static class TestDataContextFactory
{
    public static IdentityDataContext Create(string connectionString)
    {
        return new IdentityDataContext(
            new DbContextOptionsBuilder<IdentityDataContext>()
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .Options);
    }
}
