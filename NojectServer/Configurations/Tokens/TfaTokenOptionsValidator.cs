using Microsoft.Extensions.Options;

namespace NojectServer.Configurations.Tokens;

/// <summary>
/// Validates two-factor authentication token configuration options.
/// </summary>
public sealed class TfaTokenOptionsValidator : IValidateOptions<TfaTokenOptions>
{
    private const int MinimumExpirationInMinutes = 1;
    private const int MaximumExpirationInMinutes = 10;

    /// <summary>
    /// Validates the configured two-factor authentication token options.
    /// </summary>
    /// <param name="name">The named options instance being validated.</param>
    /// <param name="options">The two-factor authentication token options to validate.</param>
    /// <returns>The validation result containing success or configuration errors.</returns>
    public ValidateOptionsResult Validate(string? name, TfaTokenOptions options)
    {
        List<string> errors = [];

        JwtSigningKeyValidator.Validate(
            options.SecretKey,
            $"{TfaTokenOptions.SectionName}:SecretKey",
            errors);

        if (options.ExpirationInMinutes is < MinimumExpirationInMinutes or > MaximumExpirationInMinutes)
        {
            errors.Add($"{TfaTokenOptions.SectionName}:ExpirationInMinutes must be between {MinimumExpirationInMinutes} and {MaximumExpirationInMinutes}. " +
                       $"Configured value: {options.ExpirationInMinutes}.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
