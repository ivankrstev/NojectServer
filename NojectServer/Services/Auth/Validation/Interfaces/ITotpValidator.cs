namespace NojectServer.Services.Auth.Validation.Interfaces;

/// <summary>
/// Interface for validating TOTP (Time-based One-Time Password) codes
/// </summary>
public interface ITotpValidator
{
    /// <summary>
    /// Validates a TOTP code against a secret key
    /// </summary>
    /// <param name="secretKey">The Base32-encoded secret key</param>
    /// <param name="code">The code to validate</param>
    /// <returns>True if the code is valid, false otherwise</returns>
    bool ValidateCode(string secretKey, string code);
}
