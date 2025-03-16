using Microsoft.Extensions.Options;
using System.Text;
using static NojectServer.Configurations.JwtTokenOptions;

namespace NojectServer.Configurations;

public class JwtTokenOptionsValidator : IValidateOptions<JwtTokenOptions>
{
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
