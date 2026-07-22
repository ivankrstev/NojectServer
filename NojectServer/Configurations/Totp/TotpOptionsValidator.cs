using Microsoft.Extensions.Options;

namespace NojectServer.Configurations.Totp;

/// <summary>
/// Validates TOTP configuration options.
/// </summary>
public sealed class TotpOptionsValidator
    : IValidateOptions<TotpOptions>
{
    /// <summary>
    /// Validates the configured TOTP options.
    /// </summary>
    /// <param name="name">The named options instance being validated.</param>
    /// <param name="options">The TOTP options to validate.</param>
    /// <returns>The validation result containing success or configuration errors.</returns>
    public ValidateOptionsResult Validate(
        string? name,
        TotpOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            return ValidateOptionsResult.Fail(
                "TOTP issuer is required.");
        }

        if (options.Issuer.Contains(':'))
        {
            return ValidateOptionsResult.Fail(
                "TOTP issuer cannot contain a colon.");
        }

        return ValidateOptionsResult.Success;
    }
}
