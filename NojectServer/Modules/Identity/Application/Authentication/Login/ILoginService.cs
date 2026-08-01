using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Application.Authentication.Login;

/// <summary>
/// Authenticates users and completes two-factor authentication challenges.
/// </summary>
public interface ILoginService
{
    /// <summary>
    /// Authenticates a user using their email address and password.
    /// </summary>
    /// <param name="input">The login credentials supplied by the user.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// A result containing an <see cref="AuthenticatedLoginResult"/> when
    /// authentication completes immediately, or a
    /// <see cref="TwoFactorRequiredLoginResult"/> when an additional
    /// two-factor authentication code is required.
    /// </returns>
    Task<Result<LoginResult>> LoginAsync(
        LoginInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a pending login by validating the supplied two-factor
    /// authentication challenge and code.
    /// </summary>
    /// <param name="input">The temporary two-factor token and authenticator code supplied by the user.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// A result containing the issued access and refresh tokens when the
    /// two-factor authentication challenge is completed successfully.
    /// </returns>
    Task<Result<AuthenticatedLoginResult>> CompleteTwoFactorLoginAsync(
        CompleteTwoFactorLoginInput input,
        CancellationToken cancellationToken = default);
}
