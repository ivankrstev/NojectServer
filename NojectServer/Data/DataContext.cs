using Microsoft.EntityFrameworkCore;
using NojectServer.Modules.Identity.Domain;

namespace NojectServer.Data;

public class DataContext(
    DbContextOptions<DataContext> options)
    : DbContext(options)
{
    public virtual DbSet<User> Users { get; set; } = null!;
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            // Persist the owned arrays while exposing read-only views to callers.
            entity.Ignore(user => user.PasswordHash);
            entity.Ignore(user => user.PasswordSalt);
            entity.Ignore(user => user.VerificationTokenHash);
            entity.Ignore(user => user.PasswordResetTokenHash);
            entity.Ignore(user => user.ProtectedTwoFactorSecret);

            entity.Property<byte[]>("_passwordHash")
                .HasColumnName("password_hash")
                .IsRequired();

            entity.Property<byte[]>("_passwordSalt")
                .HasColumnName("password_salt")
                .IsRequired();

            entity.Property<byte[]?>("_verificationTokenHash")
                .HasColumnName("verification_token_hash")
                .HasMaxLength(32)
                .IsRequired(false);

            entity.Property<byte[]?>("_passwordResetTokenHash")
                .HasColumnName("password_reset_token_hash")
                .HasMaxLength(32)
                .IsRequired(false);

            entity.Property<byte[]?>("_protectedTwoFactorSecret")
                .HasColumnName("protected_two_factor_secret")
                .IsRequired(false);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Ignore(refreshToken => refreshToken.TokenHash);

            entity.Property<byte[]>("_tokenHash")
                .HasColumnName("token_hash")
                .HasMaxLength(32)
                .IsRequired();

            entity.HasIndex("_tokenHash")
                .HasDatabaseName("ix_refresh_tokens_token_hash")
                .IsUnique();

            entity.HasOne(refreshToken => refreshToken.User)
                .WithMany()
                .HasForeignKey(refreshToken => refreshToken.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
