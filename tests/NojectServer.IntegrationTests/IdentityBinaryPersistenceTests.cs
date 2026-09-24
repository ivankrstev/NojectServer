using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NojectServer.Configurations.Tokens;
using NojectServer.Data;
using NojectServer.IntegrationTests.Database;
using NojectServer.Modules.Identity.Application.RefreshTokens;
using NojectServer.Modules.Identity.Domain;
using NojectServer.Modules.Identity.Infrastructure.Persistence;
using NojectServer.Modules.Identity.Infrastructure.RefreshTokens;
using NojectServer.Utils.ResultPattern;

namespace NojectServer.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class IdentityBinaryPersistenceTests(PostgreSqlFixture postgres)
{
    private readonly PostgreSqlFixture _postgres = postgres;

    [Fact]
    public async Task BinaryFields_RoundTripAndSupportCredentialAndRefreshTokenWorkflows()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await using DataContext context = _postgres.CreateContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        byte[] passwordHash = RandomNumberGenerator.GetBytes(32);
        byte[] passwordSalt = RandomNumberGenerator.GetBytes(32);
        byte[] verificationHash = RandomNumberGenerator.GetBytes(32);
        byte[] resetHash = RandomNumberGenerator.GetBytes(32);
        byte[] protectedSecret = RandomNumberGenerator.GetBytes(64);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        User user = User.Create(
            $"binary-{Guid.NewGuid():N}@example.com", "Persistence Test",
            passwordHash, passwordSalt);
        user.SetEmailVerificationToken(verificationHash, now, now.AddHours(1));
        user.SetPasswordResetToken(resetHash, now, now.AddHours(1));
        user.SetProtectedTwoFactorSecret(protectedSecret);
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        Guid userId = user.Id;
        context.ChangeTracker.Clear();

        var repository = new UserRepository(context);
        user = Assert.IsType<User>(
            await repository.GetByPasswordResetTokenHashAsync(resetHash, cancellationToken));
        Assert.Equal(userId, user.Id);
        Assert.Equal(passwordHash, user.PasswordHash.ToArray());
        Assert.Equal(passwordSalt, user.PasswordSalt.ToArray());
        Assert.Equal(verificationHash, user.VerificationTokenHash?.ToArray());
        Assert.Equal(resetHash, user.PasswordResetTokenHash?.ToArray());
        Assert.Equal(protectedSecret, user.ProtectedTwoFactorSecret?.ToArray());

        byte[] newPasswordHash = RandomNumberGenerator.GetBytes(32);
        byte[] newPasswordSalt = RandomNumberGenerator.GetBytes(32);
        byte[] newVerificationHash = RandomNumberGenerator.GetBytes(32);
        byte[] newResetHash = RandomNumberGenerator.GetBytes(32);
        byte[] newProtectedSecret = RandomNumberGenerator.GetBytes(64);
        user.ChangePassword(newPasswordHash, newPasswordSalt);
        user.SetEmailVerificationToken(newVerificationHash, now, now.AddHours(1));
        user.SetPasswordResetToken(newResetHash, now, now.AddHours(1));
        user.SetProtectedTwoFactorSecret(newProtectedSecret);
        await repository.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();

        Assert.Null(await repository.GetByPasswordResetTokenHashAsync(resetHash, cancellationToken));
        user = Assert.IsType<User>(
            await repository.GetByPasswordResetTokenHashAsync(newResetHash, cancellationToken));
        Assert.Equal(newPasswordHash, user.PasswordHash.ToArray());
        Assert.Equal(newPasswordSalt, user.PasswordSalt.ToArray());
        Assert.Equal(newVerificationHash, user.VerificationTokenHash?.ToArray());
        Assert.Equal(newResetHash, user.PasswordResetTokenHash?.ToArray());
        Assert.Equal(newProtectedSecret, user.ProtectedTwoFactorSecret?.ToArray());

        user.ChangePassword(newPasswordHash, newPasswordSalt);
        user.MarkAsVerified(now);
        user.DisableTwoFactor();
        await repository.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();

        user = Assert.IsType<User>(await repository.GetByIdAsync(userId, cancellationToken));
        Assert.Equal(newPasswordHash, user.PasswordHash.ToArray());
        Assert.Equal(newPasswordSalt, user.PasswordSalt.ToArray());
        Assert.Null(user.VerificationTokenHash);
        Assert.Null(user.PasswordResetTokenHash);
        Assert.Null(user.ProtectedTwoFactorSecret);
        Assert.Null(await repository.GetByPasswordResetTokenHashAsync(newResetHash, cancellationToken));

        var generator = new RefreshTokenGenerator();
        var service = new RefreshTokenService(
            context, Options.Create(new RefreshTokenOptions { ExpirationInDays = 7 }),
            generator, TimeProvider.System, NullLogger<RefreshTokenService>.Instance);
        IssuedRefreshToken issued = Assert.IsType<SuccessResult<IssuedRefreshToken>>(
            await service.IssueAsync(userId, cancellationToken)).Value;
        context.ChangeTracker.Clear();

        RefreshToken original = await context.RefreshTokens.SingleAsync(
            token => token.UserId == userId, cancellationToken);
        Assert.Equal(generator.ComputeHash(issued.Token), original.TokenHash.ToArray());
        context.ChangeTracker.Clear();

        RotatedRefreshToken rotated = Assert.IsType<SuccessResult<RotatedRefreshToken>>(
            await service.RotateAsync(issued.Token, cancellationToken)).Value;
        Assert.Equal(userId, rotated.UserId);
        context.ChangeTracker.Clear();

        RefreshToken storedOriginal = await context.RefreshTokens.SingleAsync(
            token => token.Id == original.Id, cancellationToken);
        Assert.True(storedOriginal.WasRotated);
        RefreshToken replacement = await context.RefreshTokens.SingleAsync(
            token => token.Id == storedOriginal.ReplacedByTokenId, cancellationToken);
        Assert.Equal(original.FamilyId, replacement.FamilyId);
        Assert.Equal(generator.ComputeHash(rotated.Token), replacement.TokenHash.ToArray());
        context.ChangeTracker.Clear();

        Assert.True(Assert.IsType<SuccessResult<bool>>(
            await service.RevokeAsync(rotated.Token, cancellationToken)).Value);
        context.ChangeTracker.Clear();
        List<RefreshToken> family = await context.RefreshTokens
            .Where(token => token.FamilyId == original.FamilyId).ToListAsync(cancellationToken);
        Assert.Equal(2, family.Count);
        Assert.All(family, token => Assert.True(token.IsRevoked));

        await transaction.RollbackAsync(cancellationToken);
    }
}

public sealed class IdentityModelMappingTests
{
    [Fact]
    public void ReadOnlyBinaryProperties_PreserveTheExistingDatabaseSchema()
    {
        using DataContext context = TestDataContextFactory.Create(
            "Host=localhost;Database=noject_model;Username=unused;Password=unused");

        Assert.False(context.Database.HasPendingModelChanges());

        var userType = context.Model.FindEntityType(typeof(User))!;
        string[] fields =
        [
            "_passwordHash", "_passwordSalt", "_verificationTokenHash",
            "_passwordResetTokenHash", "_protectedTwoFactorSecret"
        ];

        foreach (string field in fields)
        {
            var property = userType.FindProperty(field);

            Assert.NotNull(property);
            Assert.NotNull(property.FieldInfo);
        }

        var refreshTokenType = context.Model.FindEntityType(typeof(RefreshToken))!;
        var tokenHashProperty = refreshTokenType.FindProperty("_tokenHash");
        Assert.NotNull(tokenHashProperty);
        Assert.NotNull(tokenHashProperty.FieldInfo);
        Assert.Contains(refreshTokenType.GetIndexes(), index =>
            index.IsUnique &&
            index.GetDatabaseName() == "ix_refresh_tokens_token_hash" &&
            index.Properties.Single().Name == "_tokenHash");
    }
}
