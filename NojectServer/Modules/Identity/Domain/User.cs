using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace NojectServer.Modules.Identity.Domain;

[Index(nameof(NormalizedEmail), IsUnique = true)]
public sealed class User
{
    private const int Sha256HashSizeInBytes = 32;
    internal const int MaximumFullNameLength = 50;
    internal const int MaximumEmailLength = 254;

    [Key]
    public Guid Id { get; private set; }

    [Required]
    [MaxLength(MaximumEmailLength)]
    public string Email { get; private set; } = string.Empty;

    [Required]
    [MaxLength(MaximumEmailLength)]
    public string NormalizedEmail { get; private set; } = string.Empty;

    [Required]
    [MaxLength(MaximumFullNameLength)]
    public string FullName { get; private set; } = string.Empty;

    [Required]
    public byte[] PasswordHash { get; private set; } = [];

    [Required]
    public byte[] PasswordSalt { get; private set; } = [];

    [MaxLength(Sha256HashSizeInBytes)]
    public byte[]? VerificationTokenHash { get; private set; }

    public DateTimeOffset? VerificationTokenExpiresAt { get; private set; }

    public DateTimeOffset? VerifiedAt { get; private set; }

    [MaxLength(Sha256HashSizeInBytes)]
    public byte[]? PasswordResetTokenHash { get; private set; }

    public DateTimeOffset? PasswordResetTokenExpiresAt { get; private set; }

    public bool TwoFactorEnabled { get; private set; }

    /// <summary>
    /// The protected (encrypted) two-factor authentication secret for the user.
    /// </summary>
    public byte[]? ProtectedTwoFactorSecret { get; private set; }

    /// <summary>
    /// The last time step for which a TOTP code was accepted for this user.
    /// This is used to prevent replay attacks by ensuring that the same TOTP code cannot be used more than once within the same time step.
    /// </summary>
    [ConcurrencyCheck]
    public long? LastAcceptedTotpTimeStep { get; private set; }

    private User()
    {
        // Required by EF Core.
    }

    private User(
        Guid id,
        string email,
        string fullName,
        byte[] passwordHash,
        byte[] passwordSalt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(id));
        }

        ValidatePasswordCredentials(passwordHash, passwordSalt);

        Id = id;
        SetEmail(email);

        FullName = NormalizeFullName(fullName);
        PasswordHash = (byte[])passwordHash.Clone();
        PasswordSalt = (byte[])passwordSalt.Clone();
    }

    public static User Create(
        string email,
        string fullName,
        byte[] passwordHash,
        byte[] passwordSalt)
    {
        return new User(
            id: Guid.NewGuid(),
            email: email,
            fullName: fullName,
            passwordHash: passwordHash,
            passwordSalt: passwordSalt);
    }

    // Email management

    public void ChangeEmail(string email)
    {
        SetEmail(email);

        // Changing the email normally requires verification again.
        VerifiedAt = null;
        VerificationTokenHash = null;
        VerificationTokenExpiresAt = null;
    }

    public void SetEmailVerificationToken(
        byte[] tokenHash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        ValidateSha256Hash(tokenHash, nameof(tokenHash));

        if (expiresAt <= issuedAt)
        {
            throw new ArgumentException(
                "The token expiration time must be later than its issue time.",
                nameof(expiresAt));
        }

        VerificationTokenHash = (byte[])tokenHash.Clone();
        VerificationTokenExpiresAt = expiresAt;
    }

    public void MarkAsVerified(DateTimeOffset verifiedAt)
    {
        VerifiedAt = verifiedAt;
        VerificationTokenHash = null;
        VerificationTokenExpiresAt = null;
    }

    // Password management

    public void ChangePassword(
        byte[] passwordHash,
        byte[] passwordSalt)
    {
        ValidatePasswordCredentials(passwordHash, passwordSalt);

        PasswordHash = (byte[])passwordHash.Clone();
        PasswordSalt = (byte[])passwordSalt.Clone();

        PasswordResetTokenHash = null;
        PasswordResetTokenExpiresAt = null;
    }

    public void SetPasswordResetToken(
        byte[] tokenHash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        ValidateSha256Hash(tokenHash, nameof(tokenHash));

        if (expiresAt <= issuedAt)
        {
            throw new ArgumentException(
                "The token expiration time must be later than its issue time.",
                nameof(expiresAt));
        }

        PasswordResetTokenHash = (byte[])tokenHash.Clone();
        PasswordResetTokenExpiresAt = expiresAt;
    }

    // Two-factor authentication

    public void SetProtectedTwoFactorSecret(byte[] protectedSecret)
    {
        ArgumentNullException.ThrowIfNull(protectedSecret);

        if (protectedSecret.Length == 0)
        {
            throw new ArgumentException(
                "Protected two-factor secret cannot be empty.",
                nameof(protectedSecret));
        }

        ProtectedTwoFactorSecret = (byte[])protectedSecret.Clone();
        TwoFactorEnabled = false;
        LastAcceptedTotpTimeStep = null;
    }

    public void EnableTwoFactor()
    {
        if (ProtectedTwoFactorSecret is null)
        {
            throw new InvalidOperationException(
                "A two-factor secret must be configured first.");
        }

        TwoFactorEnabled = true;
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        ProtectedTwoFactorSecret = null;
        LastAcceptedTotpTimeStep = null;
    }

    public void RecordAcceptedTotpTimeStep(long timeStep)
    {
        if (timeStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeStep));
        }

        if (LastAcceptedTotpTimeStep is long previous
            && timeStep <= previous)
        {
            throw new InvalidOperationException(
                "The TOTP time step has already been accepted.");
        }

        LastAcceptedTotpTimeStep = timeStep;
    }

    // Private helper methods

    private void SetEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        string trimmedEmail = email.Trim();

        if (trimmedEmail.Length > MaximumEmailLength)
        {
            throw new ArgumentException(
                $"Email cannot exceed {MaximumEmailLength} characters.",
                nameof(email));
        }

        Email = trimmedEmail;
        NormalizedEmail = trimmedEmail.ToUpperInvariant();
    }

    private static string NormalizeFullName(string fullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        string trimmedFullName = fullName.Trim();

        if (trimmedFullName.Length > MaximumFullNameLength)
        {
            throw new ArgumentException(
                $"Full name cannot exceed {MaximumFullNameLength} characters.",
                nameof(fullName));
        }

        return trimmedFullName;
    }

    private static void ValidatePasswordCredentials(
        byte[] passwordHash,
        byte[] passwordSalt)
    {
        ArgumentNullException.ThrowIfNull(passwordHash);
        ArgumentNullException.ThrowIfNull(passwordSalt);

        if (passwordHash.Length == 0)
        {
            throw new ArgumentException(
                "Password hash cannot be empty.",
                nameof(passwordHash));
        }

        if (passwordSalt.Length == 0)
        {
            throw new ArgumentException(
                "Password salt cannot be empty.",
                nameof(passwordSalt));
        }
    }

    private static void ValidateSha256Hash(
        byte[] hash,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(hash);

        if (hash.Length != Sha256HashSizeInBytes)
        {
            throw new ArgumentException(
                $"A SHA-256 hash must contain exactly " +
                $"{Sha256HashSizeInBytes} bytes.",
                parameterName);
        }
    }
}
