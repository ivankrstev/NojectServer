using Microsoft.AspNetCore.DataProtection;
using NojectServer.Modules.Identity.Application.Authentication.TwoFactorAuthentication;

namespace NojectServer.Modules.Identity.Infrastructure.TwoFactorAuthentication;

/// <summary>
/// Protects two-factor authentication secrets using ASP.NET Core Data Protection,
/// with each payload cryptographically bound to its owning user.
/// </summary>
internal sealed class TwoFactorSecretProtector(
    IDataProtectionProvider dataProtectionProvider
) : ITwoFactorSecretProtector
{
    private const string Purpose = "NojectServer.Identity.TwoFactorSecret.v1";

    private readonly IDataProtectionProvider _dataProtectionProvider =
        dataProtectionProvider;

    /// <inheritdoc />
    public byte[] Protect(Guid userId, byte[] secret)
    {
        ValidateUserId(userId);
        ValidateValue(secret, nameof(secret));

        IDataProtector protector = CreateProtector(userId);

        return protector.Protect(secret);
    }

    /// <inheritdoc />
    public byte[] Unprotect(Guid userId, byte[] protectedSecret)
    {
        ValidateUserId(userId);
        ValidateValue(protectedSecret, nameof(protectedSecret));

        IDataProtector protector = CreateProtector(userId);

        return protector.Unprotect(protectedSecret);
    }

    // Create a data protector with a unique purpose for the user ID
    // This ensures that the protected secrets are tied to the specific user and cannot be used interchangeably.
    private IDataProtector CreateProtector(Guid userId)
    {
        return _dataProtectionProvider.CreateProtector(
            Purpose,
            userId.ToString("N"));
    }

    // Validate that the user ID is not empty
    private static void ValidateUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));
        }
    }

    // Validate that the value is not null or empty (the secret or protected secret)
    private static void ValidateValue(
        byte[] value,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
        {
            throw new ArgumentException(
                "The value cannot be empty.",
                parameterName);
        }
    }
}
