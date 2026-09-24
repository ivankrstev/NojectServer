using Microsoft.EntityFrameworkCore;
using NojectServer.Modules.Identity.Infrastructure.Persistence;
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

    internal IdentityDataContext CreateContext()
    {
        return TestDataContextFactory.Create(ConnectionString);
    }

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using IdentityDataContext context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync()
    {
        return _container.DisposeAsync();
    }
}
