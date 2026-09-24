using Microsoft.EntityFrameworkCore;
using NojectServer.Data;
using Testcontainers.PostgreSql;

namespace NojectServer.IntegrationTests.Database;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:16.4-alpine")
            .WithDatabase("noject_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public string ConnectionString => _container.GetConnectionString();

    public DataContext CreateContext()
    {
        return TestDataContextFactory.Create(ConnectionString);
    }

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using DataContext context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync()
    {
        return _container.DisposeAsync();
    }
}
