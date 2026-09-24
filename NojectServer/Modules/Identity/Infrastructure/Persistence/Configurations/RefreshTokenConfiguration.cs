using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NojectServer.Modules.Identity.Domain;

namespace NojectServer.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration
    : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> entity)
    {
        entity.Ignore(refreshToken => refreshToken.TokenHash);

        entity.Property<byte[]>("_tokenHash")
            .HasColumnName("token_hash")
            .HasMaxLength(RefreshToken.TokenHashLength)
            .IsRequired();

        entity.HasIndex("_tokenHash")
            .HasDatabaseName("ix_refresh_tokens_token_hash")
            .IsUnique();

        entity.HasOne(refreshToken => refreshToken.User)
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
