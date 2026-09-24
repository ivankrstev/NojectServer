using Microsoft.EntityFrameworkCore;
using NojectServer.Modules.Identity.Domain;
using NojectServer.Modules.Identity.Infrastructure.Persistence.Configurations;

namespace NojectServer.Modules.Identity.Infrastructure.Persistence;

internal sealed class IdentityDataContext(
    DbContextOptions<IdentityDataContext> options)
    : DbContext(options)
{
    internal DbSet<User> Users => Set<User>();

    internal DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
    }
}
