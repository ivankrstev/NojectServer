using Microsoft.Extensions.Options;

namespace NojectServer.Configurations.Tokens;

/// <summary>
/// Validates pending-email-verification token configuration.
/// </summary>
public sealed class PendingEmailVerificationTokenOptionsValidator
    : IValidateOptions<PendingEmailVerificationTokenOptions>
{
    private const int MinimumExpirationInMinutes = 5;
    private const int MaximumExpirationInMinutes = 30;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(
        string? name,
        PendingEmailVerificationTokenOptions options)
    {
        List<string> errors = [];

        JwtSigningKeyValidator.Validate(
            options.SecretKey,
            $"{PendingEmailVerificationTokenOptions.SectionName}:SecretKey",
            errors);

        if (options.ExpirationInMinutes is
            < MinimumExpirationInMinutes or > MaximumExpirationInMinutes)
        {
            errors.Add(
                $"{PendingEmailVerificationTokenOptions.SectionName}:ExpirationInMinutes " +
                $"must be between {MinimumExpirationInMinutes} and " +
                $"{MaximumExpirationInMinutes}. Configured value: " +
                $"{options.ExpirationInMinutes}.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
