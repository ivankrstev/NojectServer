using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using NojectServer.Configurations.Totp;
using NojectServer.Modules.Identity.Application.Authentication.TwoFactorAuthentication;
using OtpNet;

namespace NojectServer.Modules.Identity.Infrastructure.TwoFactorAuthentication;

/// <summary>
/// Implements TOTP enrollment and verification using Otp.NET.
/// </summary>
internal sealed class OtpNetTotpService(
    IOptions<TotpOptions> options,
    TimeProvider timeProvider) : ITotpService
{
    private const int SecretSizeInBytes = 20;
    private const int TimeStepInSeconds = 30;
    internal const int CodeSizeInDigits = 6;

    private readonly string _issuer = options.Value.Issuer;
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <inheritdoc />
    public byte[] GenerateSecret()
    {
        return RandomNumberGenerator.GetBytes(SecretSizeInBytes);
    }

    /// <inheritdoc />
    public string EncodeSecret(byte[] secret)
    {
        ValidateSecret(secret);

        return Base32Encoding.ToString(secret);
    }

    /// <inheritdoc />
    public string CreateProvisioningUri(
        byte[] secret,
        string accountName)
    {
        ValidateSecret(secret);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);

        return new OtpUri(
            schema: OtpType.Totp,
            secret: secret,
            user: accountName.Trim(),
            issuer: _issuer.Trim(),
            algorithm: OtpHashMode.Sha1,
            digits: CodeSizeInDigits,
            period: TimeStepInSeconds).ToString();
    }

    /// <inheritdoc />
    public bool TryValidateCode(
        byte[] secret,
        string? code,
        out long matchedTimeStep)
    {
        ValidateSecret(secret);

        matchedTimeStep = default;

        if (!TryNormalizeCode(
                code,
                out string normalizedCode))
        {
            return false;
        }

        var totp = new Totp(
            secretKey: secret,
            step: TimeStepInSeconds,
            mode: OtpHashMode.Sha1,
            totpSize: CodeSizeInDigits);

        return totp.VerifyTotp(
            timestamp: _timeProvider.GetUtcNow().UtcDateTime,
            totp: normalizedCode,
            out matchedTimeStep,
            window: VerificationWindow.RfcSpecifiedNetworkDelay);
    }

    // Validates and normalizes the provided TOTP code.
    private static bool TryNormalizeCode(
        string? code,
        out string normalizedCode)
    {
        normalizedCode = string.Empty;

        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        string candidate = code.Trim();

        if (candidate.Length != CodeSizeInDigits || !candidate.All(char.IsDigit))
        {
            return false;
        }

        normalizedCode = candidate;

        return true;
    }

    // Validates that the provided TOTP secret is not null or empty.
    private static void ValidateSecret(byte[] secret)
    {
        ArgumentNullException.ThrowIfNull(secret);

        if (secret.Length == 0)
        {
            throw new ArgumentException(
                "TOTP secret cannot be empty.",
                nameof(secret));
        }
    }
}
