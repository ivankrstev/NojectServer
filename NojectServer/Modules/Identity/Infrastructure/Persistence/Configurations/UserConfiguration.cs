using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NojectServer.Modules.Identity.Domain;

namespace NojectServer.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration
    : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
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
            .HasMaxLength(User.Sha256HashSizeInBytes)
            .IsRequired(false);

        entity.Property<byte[]?>("_passwordResetTokenHash")
            .HasColumnName("password_reset_token_hash")
            .HasMaxLength(User.Sha256HashSizeInBytes)
            .IsRequired(false);

        entity.Property<byte[]?>("_protectedTwoFactorSecret")
            .HasColumnName("protected_two_factor_secret")
            .IsRequired(false);
    }
}
