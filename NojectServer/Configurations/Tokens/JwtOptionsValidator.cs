using Microsoft.Extensions.Options;

namespace NojectServer.Configurations.Tokens;

/// <summary>
/// Validates JWT configuration options.
/// </summary>
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    private const int MaximumClockSkewInSeconds = 60;

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

        if (options.ClockSkewInSeconds is null)
        {
            errors.Add($"{JwtOptions.SectionName}:ClockSkewInSeconds is required");
        }
        else if (options.ClockSkewInSeconds is < 0 or > MaximumClockSkewInSeconds)
        {
            errors.Add($"{JwtOptions.SectionName}:ClockSkewInSeconds must be between 0 and {MaximumClockSkewInSeconds}.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
