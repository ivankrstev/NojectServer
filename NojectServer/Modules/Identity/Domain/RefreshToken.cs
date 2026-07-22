using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NojectServer.Modules.Identity.Domain;

[Index(nameof(TokenHash), IsUnique = true)]
[Index(nameof(UserId), nameof(FamilyId))]
public sealed class RefreshToken
{
    private const int TokenHashLength = 32;

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; private set; }

    [ForeignKey(nameof(User))]
    public Guid UserId { get; private set; }

    /// <summary>
    /// SHA-256 hash of the bearer token. The raw token must never be persisted.
    /// </summary>
    [Required]
    [MaxLength(TokenHashLength)]
    public byte[] TokenHash { get; private set; } = [];

    /// <summary>
    /// Identifies all refresh tokens belonging to the same login session.
    /// </summary>
    public Guid FamilyId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// Identifies the token issued when this token was rotated.
    /// </summary>
    public Guid? ReplacedByTokenId { get; private set; }

    /// <summary>
    /// Application-managed optimistic concurrency token.
    /// This is unrelated to the authentication token value.
    /// </summary>
    [ConcurrencyCheck]
    public Guid ConcurrencyToken { get; private set; }

    public User User { get; private set; } = null!;

    private RefreshToken()
    {
        // Required by EF Core
    }

    private RefreshToken(
        Guid id,
        Guid userId,
        byte[] tokenHash,
        Guid familyId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "ID cannot be empty.",
                nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));
        }

        if (familyId == Guid.Empty)
        {
            throw new ArgumentException(
                "Family ID cannot be empty.",
                nameof(familyId));
        }

        ArgumentNullException.ThrowIfNull(tokenHash, nameof(tokenHash));

        if (tokenHash.Length != TokenHashLength)
        {
            throw new ArgumentException(
                $"Token hash must be exactly {TokenHashLength} bytes.",
                nameof(tokenHash));
        }

        if (expiresAt <= createdAt)
        {
            throw new ArgumentException(
                "Expiration time must be after creation time.",
                nameof(expiresAt));
        }

        Id = id;
        UserId = userId;

        // Prevent the caller from modifying our stored array.
        TokenHash = (byte[])tokenHash.Clone();

        FamilyId = familyId;
        ConcurrencyToken = Guid.NewGuid();

        // Npgsql expects DateTimeOffset values written to timestamptz
        // to have an offset of zero (UTC).
        CreatedAt = createdAt.ToUniversalTime();
        ExpiresAt = expiresAt.ToUniversalTime();
    }

    /// <summary>
    /// Creates the first refresh token for a new login session.
    /// </summary>
    public static RefreshToken CreateNewSession(
        Guid userId,
        byte[] tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        return new RefreshToken(
            id: Guid.NewGuid(),
            userId,
            tokenHash,
            familyId: Guid.NewGuid(),
            createdAt,
            expiresAt);
    }

    /// <summary>
    /// Creates a replacement token in the same login-session family.
    /// </summary>
    public RefreshToken CreateReplacement(
        byte[] newTokenHash,
        DateTimeOffset createdAt)
    {
        if (createdAt < CreatedAt)
        {
            throw new ArgumentException(
                "The replacement token cannot be created before the current token.",
                nameof(createdAt));
        }

        if (!IsActive(createdAt))
        {
            throw new InvalidOperationException("Only an active refresh token can be replaced.");
        }

        return new RefreshToken(
            id: Guid.NewGuid(),
            userId: UserId,
            tokenHash: newTokenHash,
            familyId: FamilyId,
            createdAt: createdAt,

            // Preserve the original session expiration so repeated
            // rotations cannot extend the session indefinitely.
            expiresAt: ExpiresAt);
    }

    public bool IsExpired(DateTimeOffset now)
    {
        return now >= ExpiresAt;
    }

    public bool IsActive(DateTimeOffset now)
    {
        return !IsRevoked && !IsExpired(now);
    }


    public bool IsRevoked => RevokedAt is not null;

    public bool WasRotated => ReplacedByTokenId is not null;

    /// <summary>
    /// Consumes this token and links it to its replacement.
    /// </summary>
    public void MarkAsRotated(
        Guid replacementTokenId,
        DateTimeOffset rotatedAt)
    {
        if (replacementTokenId == Guid.Empty)
        {
            throw new ArgumentException(
                "Replacement token ID cannot be empty.",
                nameof(replacementTokenId));
        }

        if (rotatedAt < CreatedAt)
        {
            throw new ArgumentException(
                "The rotation time cannot be before the token creation time.",
                nameof(rotatedAt));
        }

        if (!IsActive(rotatedAt))
        {
            throw new InvalidOperationException(
                "Only an active refresh token can be rotated.");
        }

        RevokedAt = rotatedAt;
        ReplacedByTokenId = replacementTokenId;
        ConcurrencyToken = Guid.NewGuid();
    }

    /// <summary>
    /// Revokes this refresh token without deleting its rotation history.
    /// </summary>
    public void Revoke(DateTimeOffset revokedAt)
    {
        if (revokedAt < CreatedAt)
        {
            throw new ArgumentException(
                "The revocation time cannot be before the token creation time.",
                nameof(revokedAt));
        }

        // Revocation is idempotent.
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = revokedAt;
        ConcurrencyToken = Guid.NewGuid();
    }
}
