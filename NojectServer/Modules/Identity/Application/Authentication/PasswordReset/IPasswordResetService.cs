using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Application.Authentication.PasswordReset;

/// <summary>
/// Coordinates password-reset requests and password changes.
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Creates and emails a password-reset token when the supplied account exists.
    /// </summary>
    /// <remarks>
    /// A valid request succeeds regardless of whether the email is registered,
    /// preventing account enumeration.
    /// </remarks>
    /// <param name="input">The email requesting a password reset.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A result indicating whether the request was accepted.</returns>
    Task<Result> RequestResetAsync(
        RequestPasswordResetInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a user's password after validating a single-use reset token.
    /// </summary>
    /// <param name="input">The reset token and new password information.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A result indicating whether the password was reset.</returns>
    Task<Result> ResetPasswordAsync(
        ResetPasswordInput input,
        CancellationToken cancellationToken = default);
}
