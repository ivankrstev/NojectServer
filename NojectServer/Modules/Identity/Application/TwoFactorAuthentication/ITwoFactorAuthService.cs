using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Application.TwoFactorAuthentication;

/// <summary>
/// Manages TOTP enrollment, activation, validation, and removal for users.
/// </summary>
public interface ITwoFactorAuthService
{
    /// <summary>
    /// Generates and stores a new protected TOTP secret for a user.
    /// </summary>
    /// <param name="userId">The ID of the user starting enrollment.</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>The manual key and provisioning URI needed to configure an authenticator.</returns>
    Task<Result<TwoFactorSetup>> GenerateSetupCodeAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an enrollment code and enables two-factor authentication.
    /// </summary>
    /// <param name="userId">The ID of the user enabling two-factor authentication.</param>
    /// <param name="code">The code supplied by the user's authenticator.</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>A success message when two-factor authentication is enabled.</returns>
    Task<Result<string>> EnableAsync(
        Guid userId,
        string? code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a code and removes two-factor authentication from a user.
    /// </summary>
    /// <param name="userId">The ID of the user disabling two-factor authentication.</param>
    /// <param name="code">The code supplied by the user's authenticator.</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>A success message when two-factor authentication is disabled.</returns>
    Task<Result<string>> DisableAsync(
        Guid userId,
        string? code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and consumes a TOTP code during authentication.
    /// </summary>
    /// <param name="userId">The ID of the user whose code is being validated.</param>
    /// <param name="code">The code supplied by the user's authenticator.</param>
    /// <param name="cancellationToken">Token used to cancel the database operation.</param>
    /// <returns>A successful result containing <see langword="true" /> when the code is accepted.</returns>
    Task<Result<bool>> ValidateCodeAsync(
        Guid userId,
        string? code,
        CancellationToken cancellationToken = default);
}
