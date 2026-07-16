using Microsoft.Extensions.Options;

namespace NojectServer.Configurations.Tokens;

/// <summary>
/// Validates access token configuration options.
/// </summary>
public sealed class AccessTokenOptionsValidator : IValidateOptions<AccessTokenOptions>
{
    private const int MinimumExpirationInMinutes = 1;
    private const int MaximumExpirationInMinutes = 30;

    /// <summary>
    /// Validates the configured access token options.
    /// </summary>
    /// <param name="name">The named options instance being validated.</param>
    /// <param name="options">The access token options to validate.</param>
    /// <returns>The validation result containing success or configuration errors.</returns>
    public ValidateOptionsResult Validate(string? name, AccessTokenOptions options)
    {
        List<string> errors = [];

        JwtSigningKeyValidator.Validate(
            options.SecretKey,
            $"{AccessTokenOptions.SectionName}:SecretKey",
            errors);

        if (options.ExpirationInMinutes is < MinimumExpirationInMinutes or > MaximumExpirationInMinutes)
        {
            errors.Add($"{AccessTokenOptions.SectionName}:ExpirationInMinutes must be between {MinimumExpirationInMinutes} and {MaximumExpirationInMinutes}. " +
                       $"Configured value: {options.ExpirationInMinutes}.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
