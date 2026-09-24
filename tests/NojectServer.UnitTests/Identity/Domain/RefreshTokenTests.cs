using NojectServer.Modules.Identity.Domain;

namespace NojectServer.UnitTests.Identity.Domain;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 21, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset ExpiresAt =
        CreatedAt.AddHours(1);

    [Fact]
    public void CreateNewSession_InitializesTokenAndSessionState()
    {
        Guid userId = Guid.NewGuid();
        byte[] tokenHash = CreateHash(1);

        RefreshToken token = RefreshToken.CreateNewSession(
            userId,
            tokenHash,
            CreatedAt,
            ExpiresAt);

        Assert.NotEqual(Guid.Empty, token.Id);
        Assert.Equal(userId, token.UserId);
        Assert.Equal(tokenHash, token.TokenHash.ToArray());
        Assert.NotEqual(Guid.Empty, token.FamilyId);
        Assert.Equal(CreatedAt, token.CreatedAt);
        Assert.Equal(ExpiresAt, token.ExpiresAt);
        Assert.Null(token.RevokedAt);
        Assert.Null(token.ReplacedByTokenId);
        Assert.NotEqual(Guid.Empty, token.ConcurrencyToken);
        Assert.False(token.IsExpired(CreatedAt));
        Assert.True(token.IsActive(CreatedAt));
        Assert.False(token.IsRevoked);
        Assert.False(token.WasRotated);
    }

    [Fact]
    public void CreateNewSession_NormalizesTimesToUtc()
    {
        DateTimeOffset createdAt = new(2026, 9, 21, 12, 0, 0, TimeSpan.FromHours(2));
        DateTimeOffset expiresAt = createdAt.AddHours(1);

        RefreshToken token = RefreshToken.CreateNewSession(
            Guid.NewGuid(),
            CreateHash(1),
            createdAt,
            expiresAt);

        Assert.Equal(createdAt.ToUniversalTime(), token.CreatedAt);
        Assert.Equal(expiresAt.ToUniversalTime(), token.ExpiresAt);
        Assert.Equal(TimeSpan.Zero, token.CreatedAt.Offset);
        Assert.Equal(TimeSpan.Zero, token.ExpiresAt.Offset);
    }

    [Fact]
    public void CreateNewSession_ClonesTokenHash()
    {
        byte[] tokenHash = CreateHash(1);
        RefreshToken token = RefreshToken.CreateNewSession(
            Guid.NewGuid(),
            tokenHash,
            CreatedAt,
            ExpiresAt);

        tokenHash[0] = 99;

        Assert.Equal(CreateHash(1), token.TokenHash.ToArray());
    }

    [Fact]
    public void CreateNewSession_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentException>(() => RefreshToken.CreateNewSession(
            Guid.Empty,
            CreateHash(1),
            CreatedAt,
            ExpiresAt));

        Assert.Throws<ArgumentNullException>(() => RefreshToken.CreateNewSession(
            Guid.NewGuid(),
            null!,
            CreatedAt,
            ExpiresAt));

        Assert.Throws<ArgumentException>(() => RefreshToken.CreateNewSession(
            Guid.NewGuid(),
            [],
            CreatedAt,
            ExpiresAt));

        Assert.Throws<ArgumentException>(() => RefreshToken.CreateNewSession(
            Guid.NewGuid(),
            new byte[31],
            CreatedAt,
            ExpiresAt));

        Assert.Throws<ArgumentException>(() => RefreshToken.CreateNewSession(
            Guid.NewGuid(),
            new byte[33],
            CreatedAt,
            ExpiresAt));

        Assert.Throws<ArgumentException>(() => RefreshToken.CreateNewSession(
            Guid.NewGuid(),
            CreateHash(1),
            CreatedAt,
            CreatedAt));

        Assert.Throws<ArgumentException>(() => RefreshToken.CreateNewSession(
            Guid.NewGuid(),
            CreateHash(1),
            CreatedAt,
            CreatedAt.AddMinutes(-1)));
    }

    [Fact]
    public void IsExpired_UsesExpirationBoundary()
    {
        RefreshToken token = CreateToken();

        Assert.False(token.IsExpired(CreatedAt.AddMinutes(30)));
        Assert.True(token.IsExpired(ExpiresAt));
        Assert.True(token.IsExpired(ExpiresAt.AddTicks(1)));
    }

    [Fact]
    public void IsActive_IsFalseWhenExpiredOrRevoked()
    {
        RefreshToken expiredToken = CreateToken();
        Assert.False(expiredToken.IsActive(ExpiresAt));

        RefreshToken revokedToken = CreateToken();
        revokedToken.Revoke(CreatedAt.AddMinutes(5));

        Assert.False(revokedToken.IsActive(CreatedAt.AddMinutes(5)));
        Assert.False(revokedToken.IsActive(CreatedAt.AddMinutes(30)));
    }

    [Fact]
    public void CreateReplacement_PreservesUserFamilyAndSessionExpiration()
    {
        RefreshToken currentToken = CreateToken();
        byte[] replacementHash = CreateHash(2);
        DateTimeOffset replacementCreatedAt = CreatedAt.AddMinutes(5);

        RefreshToken replacementToken = currentToken.CreateReplacement(
            replacementHash,
            replacementCreatedAt);

        Assert.NotEqual(currentToken.Id, replacementToken.Id);
        Assert.Equal(currentToken.UserId, replacementToken.UserId);
        Assert.Equal(currentToken.FamilyId, replacementToken.FamilyId);
        Assert.Equal(replacementHash, replacementToken.TokenHash.ToArray());
        Assert.Equal(replacementCreatedAt, replacementToken.CreatedAt);
        Assert.Equal(currentToken.ExpiresAt, replacementToken.ExpiresAt);
        Assert.Null(replacementToken.RevokedAt);
        Assert.Null(replacementToken.ReplacedByTokenId);
        Assert.False(replacementToken.WasRotated);
        Assert.True(currentToken.IsActive(replacementCreatedAt));
    }

    [Fact]
    public void CreateReplacement_RejectsInvalidHash()
    {
        RefreshToken token = CreateToken();
        DateTimeOffset replacementCreatedAt = CreatedAt.AddMinutes(5);

        Assert.Throws<ArgumentNullException>(() => token.CreateReplacement(
            null!,
            replacementCreatedAt));
        Assert.Throws<ArgumentException>(() => token.CreateReplacement(
            new byte[31],
            replacementCreatedAt));
    }

    [Fact]
    public void CreateReplacement_RejectsCreationBeforeCurrentToken()
    {
        RefreshToken token = CreateToken();

        Assert.Throws<ArgumentException>(() => token.CreateReplacement(
            CreateHash(2),
            CreatedAt.AddTicks(-1)));
    }

    [Fact]
    public void CreateReplacement_RejectsExpiredRevokedAndRotatedTokens()
    {
        RefreshToken expiredToken = CreateToken();
        Assert.Throws<InvalidOperationException>(() => expiredToken.CreateReplacement(
            CreateHash(2),
            ExpiresAt));

        RefreshToken revokedToken = CreateToken();
        revokedToken.Revoke(CreatedAt.AddMinutes(5));
        Assert.Throws<InvalidOperationException>(() => revokedToken.CreateReplacement(
            CreateHash(2),
            CreatedAt.AddMinutes(6)));

        RefreshToken rotatedToken = CreateToken();
        rotatedToken.MarkAsRotated(Guid.NewGuid(), CreatedAt.AddMinutes(5));
        Assert.Throws<InvalidOperationException>(() => rotatedToken.CreateReplacement(
            CreateHash(2),
            CreatedAt.AddMinutes(6)));
    }

    [Fact]
    public void MarkAsRotated_RevokesAndLinksTokenToReplacement()
    {
        RefreshToken token = CreateToken();
        Guid originalConcurrencyToken = token.ConcurrencyToken;
        Guid replacementTokenId = Guid.NewGuid();
        DateTimeOffset rotatedAt = CreatedAt.AddMinutes(5);

        token.MarkAsRotated(replacementTokenId, rotatedAt);

        Assert.Equal(rotatedAt, token.RevokedAt);
        Assert.Equal(replacementTokenId, token.ReplacedByTokenId);
        Assert.True(token.IsRevoked);
        Assert.True(token.WasRotated);
        Assert.False(token.IsActive(rotatedAt));
        Assert.NotEqual(originalConcurrencyToken, token.ConcurrencyToken);
    }

    [Fact]
    public void MarkAsRotated_RejectsEmptyOrSelfReplacement()
    {
        RefreshToken token = CreateToken();

        Assert.Throws<ArgumentException>(() => token.MarkAsRotated(
            Guid.Empty,
            CreatedAt.AddMinutes(5)));
        Assert.Throws<ArgumentException>(() => token.MarkAsRotated(
            token.Id,
            CreatedAt.AddMinutes(5)));

        Assert.False(token.WasRotated);
        Assert.Null(token.RevokedAt);
    }

    [Fact]
    public void MarkAsRotated_RejectsRotationBeforeCreation()
    {
        RefreshToken token = CreateToken();

        Assert.Throws<ArgumentException>(() => token.MarkAsRotated(
            Guid.NewGuid(),
            CreatedAt.AddTicks(-1)));
    }

    [Fact]
    public void MarkAsRotated_RejectsExpiredRevokedAndAlreadyRotatedTokens()
    {
        RefreshToken expiredToken = CreateToken();
        Assert.Throws<InvalidOperationException>(() => expiredToken.MarkAsRotated(
            Guid.NewGuid(),
            ExpiresAt));

        RefreshToken revokedToken = CreateToken();
        revokedToken.Revoke(CreatedAt.AddMinutes(5));
        Assert.Throws<InvalidOperationException>(() => revokedToken.MarkAsRotated(
            Guid.NewGuid(),
            CreatedAt.AddMinutes(6)));

        RefreshToken rotatedToken = CreateToken();
        rotatedToken.MarkAsRotated(Guid.NewGuid(), CreatedAt.AddMinutes(5));
        Assert.Throws<InvalidOperationException>(() => rotatedToken.MarkAsRotated(
            Guid.NewGuid(),
            CreatedAt.AddMinutes(6)));
    }

    [Fact]
    public void Revoke_RecordsRevocationAndChangesConcurrencyToken()
    {
        RefreshToken token = CreateToken();
        Guid originalConcurrencyToken = token.ConcurrencyToken;
        DateTimeOffset revokedAt = CreatedAt.AddMinutes(5);

        token.Revoke(revokedAt);

        Assert.Equal(revokedAt, token.RevokedAt);
        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive(revokedAt));
        Assert.NotEqual(originalConcurrencyToken, token.ConcurrencyToken);
        Assert.False(token.WasRotated);
        Assert.Null(token.ReplacedByTokenId);
    }

    [Fact]
    public void Revoke_IsIdempotentAndPreservesExistingRevocationState()
    {
        RefreshToken token = CreateToken();
        DateTimeOffset firstRevocationAt = CreatedAt.AddMinutes(5);
        DateTimeOffset secondRevocationAt = CreatedAt.AddMinutes(10);

        token.Revoke(firstRevocationAt);
        Guid concurrencyTokenAfterFirstRevocation = token.ConcurrencyToken;

        token.Revoke(secondRevocationAt);

        Assert.Equal(firstRevocationAt, token.RevokedAt);
        Assert.Equal(concurrencyTokenAfterFirstRevocation, token.ConcurrencyToken);
    }

    [Fact]
    public void Revoke_PreservesRotationHistory()
    {
        RefreshToken token = CreateToken();
        Guid replacementTokenId = Guid.NewGuid();
        DateTimeOffset rotatedAt = CreatedAt.AddMinutes(5);

        token.MarkAsRotated(replacementTokenId, rotatedAt);
        token.Revoke(CreatedAt.AddMinutes(10));

        Assert.Equal(rotatedAt, token.RevokedAt);
        Assert.Equal(replacementTokenId, token.ReplacedByTokenId);
        Assert.True(token.WasRotated);
    }

    [Fact]
    public void Revoke_RejectsRevocationBeforeCreation()
    {
        RefreshToken token = CreateToken();

        Assert.Throws<ArgumentException>(() => token.Revoke(CreatedAt.AddTicks(-1)));
        Assert.Null(token.RevokedAt);
    }

    private static RefreshToken CreateToken()
    {
        return RefreshToken.CreateNewSession(
            Guid.NewGuid(),
            CreateHash(1),
            CreatedAt,
            ExpiresAt);
    }

    private static byte[] CreateHash(byte firstByte)
    {
        byte[] hash = new byte[32];
        hash[0] = firstByte;
        return hash;
    }
}
