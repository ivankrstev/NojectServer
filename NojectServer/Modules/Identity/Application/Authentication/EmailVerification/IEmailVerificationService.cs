using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Application.Authentication.EmailVerification;

/// <summary>
/// Coordinates issuing and consuming email-verification tokens.
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>
    /// Issues and emails a new verification token when an unverified account exists.
    /// </summary>
    /// <remarks>
    /// A valid request succeeds regardless of account existence or verification
    /// status, so the result does not disclose account information.
    /// </remarks>
    /// <param name="input">The email address requesting verification.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A result indicating whether the request was accepted.</returns>
    Task<Result> RequestVerificationAsync(
        RequestEmailVerificationInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an email address as verified after validating its verification token.
    /// </summary>
    /// <param name="input">The email address and verification token.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A result indicating whether the email address was verified.</returns>
    Task<Result> VerifyAsync(
        VerifyEmailInput input,
        CancellationToken cancellationToken = default);
}
