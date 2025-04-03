using NojectServer.Utils.ResultPattern;

namespace NojectServer.Services.Auth.Interfaces;

/// <summary>
/// Defines the contract for two-factor authentication (2FA) services.
/// This interface provides methods to setup, enable, disable, and validate
/// time-based one-time password (TOTP) authentication for users in the application.
/// </summary>
public interface ITwoFactorAuthService
{
    /// <summary>
    /// Generates a setup code for 2FA and prepares the user account for 2FA enrollment.
    /// This method creates a new secret key and generates a QR code URL for TOTP apps.
    /// </summary>
    /// <param name="email">The email address of the user</param>
    /// <returns>
    /// A Result containing TwoFactorSetupResult with the manual key and QR code URL if successful,
    /// or an error message if the operation fails
    /// </returns>
    Task<Result<TwoFactorSetupResult>> GenerateSetupCodeAsync(string email);

    /// <summary>
    /// Enables two-factor authentication for a user after verifying the provided code.
    /// The user must first call GenerateSetupCodeAsync before enabling 2FA.
    /// </summary>
    /// <param name="email">The email address of the user</param>
    /// <param name="code">The verification code from the user's TOTP app</param>
    /// <returns>
    /// A Result containing a success message if 2FA was enabled successfully,
    /// or an error message if the operation fails
    /// </returns>
    Task<Result<string>> EnableTwoFactorAsync(string email, string code);

    /// <summary>
    /// Disables two-factor authentication for a user after verifying the provided code.
    /// </summary>
    /// <param name="email">The email address of the user</param>
    /// <param name="code">The verification code from the user's TOTP app</param>
    /// <returns>
    /// A Result containing a success message if 2FA was disabled successfully,
    /// or an error message if the operation fails
    /// </returns>
    Task<Result<string>> DisableTwoFactorAsync(string email, string code);

    /// <summary>
    /// Validates a TOTP code provided by the user during the login process.
    /// </summary>
    /// <param name="email">The email address of the user</param>
    /// <param name="code">The verification code from the user's TOTP app</param>
    /// <returns>
    /// A Result containing a boolean indicating whether the code is valid,
    /// or an error message if the validation process fails
    /// </returns>
    Task<Result<bool>> ValidateTwoFactorCodeAsync(string email, string code);
}

/// <summary>
/// Contains the results of generating a two-factor authentication setup.
/// These values are used by the client to display setup instructions to the user.
/// </summary>
public class TwoFactorSetupResult
{
    /// <summary>
    /// The secret key in text format for manual entry into a TOTP app
    /// </summary>
    public string ManualKey { get; set; } = string.Empty;

    /// <summary>
    /// The URL for generating a QR code that can be scanned by TOTP apps
    /// </summary>
    public string QrCodeImageUrl { get; set; } = string.Empty;
}
