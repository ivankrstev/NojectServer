using Microsoft.EntityFrameworkCore;
using NojectServer.Data;

namespace NojectServer.IntegrationTests.Database;

public static class TestDataContextFactory
{
    public static DataContext Create(string connectionString)
    {
        return new DataContext(
            new DbContextOptionsBuilder<DataContext>()
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .Options);
    }
}
