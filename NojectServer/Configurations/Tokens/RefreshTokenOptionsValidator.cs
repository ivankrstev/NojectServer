using Microsoft.Extensions.Options;

namespace NojectServer.Configurations.Tokens;

/// <summary>
/// Validates refresh token configuration options.
/// </summary>
public sealed class RefreshTokenOptionsValidator : IValidateOptions<RefreshTokenOptions>
{
    private const int MaximumExpirationInDays = 90;

    /// <summary>
    /// Validates the configured refresh token options.
    /// </summary>
    /// <param name="name">The named options instance being validated.</param>
    /// <param name="options">The refresh token options to validate.</param>
    /// <returns>The validation result containing success or configuration errors.</returns>
    public ValidateOptionsResult Validate(string? name, RefreshTokenOptions options)
    {
        if (options.ExpirationInDays is <= 0 or > MaximumExpirationInDays)
        {
            return ValidateOptionsResult.Fail(
                $"{RefreshTokenOptions.SectionName}:ExpirationInDays must be between 1 and {MaximumExpirationInDays}. " +
                $"Configured value: {options.ExpirationInDays}.");
        }

        return ValidateOptionsResult.Success;
    }
}
