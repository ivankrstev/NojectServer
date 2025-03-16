using Microsoft.Extensions.Options;
using System.Text;
using static NojectServer.Configurations.JwtTokenOptions;

namespace NojectServer.Configurations;

/// <summary>
/// Validates JWT token configuration settings at application startup.
///
/// This class implements the IValidateOptions interface to check that all required JWT settings
/// are properly configured and meet security requirements. It validates the presence of issuer and audience,
/// and ensures each token type (access, refresh, and TFA) has appropriate secret keys and expiration times.
/// If validation fails, the application will not start, preventing security issues from misconfiguration.
/// </summary>
public class JwtTokenOptionsValidator : IValidateOptions<JwtTokenOptions>
{
    /// <summary>
    /// Validates the JWT token options configuration
    /// </summary>
    /// <param name="name">The name of the options being validated</param>
    /// <param name="options">The JWT token options to validate</param>
    /// <returns>A success result if the options are valid, otherwise a failure result with error messages</returns>
    public ValidateOptionsResult Validate(string? name, JwtTokenOptions options)
    {
        var errors = new List<string>();

        // Check top-level properties
        if (string.IsNullOrEmpty(options.Issuer))
        {
            errors.Add("Jwt.Issuer is missing.");
        }

        if (string.IsNullOrEmpty(options.Audience))
        {
            errors.Add("Jwt.Audience is missing.");
        }

        // Validate each token type
        errors.AddRange(ValidateTokenOptions(options.Tfa, "Tfa"));
        errors.AddRange(ValidateTokenOptions(options.Access, "Access"));
        errors.AddRange(ValidateTokenOptions(options.Refresh, "Refresh"));

        // If there are any errors, fail validation
        if (errors.Count != 0)
        {
            return ValidateOptionsResult.Fail(string.Join(Environment.NewLine, errors));
        }

        return ValidateOptionsResult.Success;
    }

    /// <summary>
    /// Validates the signing credentials for a specific JWT token type
    /// </summary>
    /// <param name="options">The signing credentials to validate</param>
    /// <param name="tokenType">The type of token (Access, Refresh, or Tfa)</param>
    /// <returns>A list of validation error messages, empty if valid</returns>
    private static List<string> ValidateTokenOptions(JwtSigningCredentials options, string tokenType)
    {
        var errors = new List<string>();

        // Check if the token options are missing
        if (options == null)
        {
            errors.Add($"{tokenType} options are missing.");
            return errors;
        }

        // Validate the secret key
        if (string.IsNullOrEmpty(options.SecretKey))
        {
            errors.Add($"{tokenType}.SecretKey is missing.");
        }
        else
        {
            var byteCount = Encoding.UTF8.GetByteCount(options.SecretKey);
            if (byteCount < 64)
            {
                errors.Add($"{tokenType}.SecretKey must be at least 64 bytes long. Current length: {byteCount} bytes.");
            }
        }

        // Optional: Validate expiration (example additional check)
        if (options.ExpirationInSeconds <= 0)
        {
            errors.Add($"{tokenType}.ExpirationInSeconds must be greater than 0.");
        }

        return errors;
    }
}
