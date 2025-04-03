using NojectServer.Services.Auth.Validation.Interfaces;
using OtpNet;

namespace NojectServer.Services.Auth.Validation.Implementations;

/// <summary>
/// Implementation of TOTP validation using OtpNet library
/// </summary>
public class OtpNetTotpValidator : ITotpValidator
{
    /// <summary>
    /// Validates a TOTP code against a secret key using OtpNet
    /// </summary>
    /// <param name="secretKey">The Base32-encoded secret key</param>
    /// <param name="code">The code to validate</param>
    /// <returns>True if the code is valid, false otherwise</returns>
    public bool ValidateCode(string secretKey, string code)
    {
        try
        {
            // Convert the Base32 secret to byte array
            var secretKeyBytes = Base32Encoding.ToBytes(secretKey);

            // Create TOTP instance with 30-second window
            var totp = new Totp(secretKeyBytes, step: 30);

            // Verify with some time drift allowance (1 period before and after)
            return totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
        }
        catch
        {
            return false;
        }
    }
}
