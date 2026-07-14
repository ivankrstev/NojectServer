namespace NojectServer.Configurations.Tokens;

/// <summary>
/// Validates JWT signing keys used by token configuration.
/// </summary>
internal static class JwtSigningKeyValidator
{
    private const int MinimumHs512KeySizeInBytes = 64;

    /// <summary>
    /// Validates that a signing key is present, Base64 encoded, and large enough for HS512.
    /// </summary>
    /// <param name="secretKey">The configured signing key.</param>
    /// <param name="configurationPath">The configuration path used in validation errors.</param>
    /// <param name="errors">The collection that receives validation errors.</param>
    public static void Validate(
        string? secretKey,
        string configurationPath,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            errors.Add($"{configurationPath} is required.");
            return;
        }

        byte[] secretKeyBytes;

        try
        {
            secretKeyBytes = Convert.FromBase64String(secretKey);
        }
        catch (FormatException)
        {
            errors.Add(
                $"{configurationPath} must be a valid Base64 value.");
            return;
        }

        if (secretKeyBytes.Length < MinimumHs512KeySizeInBytes)
        {
            errors.Add(
                $"{configurationPath} must decode to at least " +
                $"{MinimumHs512KeySizeInBytes} bytes for HS512. " +
                $"Current size: {secretKeyBytes.Length} bytes.");
        }
    }
}
