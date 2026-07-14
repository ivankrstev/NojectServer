using Microsoft.Extensions.Options;

namespace NojectServer.Configurations.Tokens;

/// <summary>
/// Validates JWT configuration options.
/// </summary>
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    /// <summary>
    /// Validates the configured JWT options.
    /// </summary>
    /// <param name="name">The named options instance being validated.</param>
    /// <param name="options">The JWT options to validate.</param>
    /// <returns>The validation result containing success or configuration errors.</returns>
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            errors.Add($"{JwtOptions.SectionName}:Issuer is required");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            errors.Add($"{JwtOptions.SectionName}:Audience is required");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
