namespace NojectServer.Modules.Identity.Application.Email;

/// <summary>
/// Defines application-level email notifications for identity workflows.
/// </summary>
/// <remarks>
/// Implementations are responsible for composing notification content and
/// delegating delivery to the configured email transport.
/// </remarks>
public interface IEmailService
{
    /// <summary>
    /// Sends an email containing a single-use email-verification link.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="fullName">The recipient's display name.</param>
    /// <param name="verificationToken">The raw, single-use verification token.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous message composition and delivery operation.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="email"/>, <paramref name="fullName"/>, or
    /// <paramref name="verificationToken"/> is empty or consists only of whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="email"/>, <paramref name="fullName"/>, or
    /// <paramref name="verificationToken"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through
    /// <paramref name="cancellationToken"/>.
    /// </exception>
    Task SendVerificationLinkAsync(
        string email,
        string fullName,
        string verificationToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email containing a single-use password-reset link.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="fullName">The recipient's display name.</param>
    /// <param name="passwordResetToken">The raw, single-use password reset token.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous message composition and delivery operation.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="email"/>, <paramref name="fullName"/>, or
    /// <paramref name="passwordResetToken"/> is empty or consists only of whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="email"/>, <paramref name="fullName"/>, or
    /// <paramref name="passwordResetToken"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through
    /// <paramref name="cancellationToken"/>.
    /// </exception>
    Task SendResetPasswordLinkAsync(
        string email,
        string fullName,
        string passwordResetToken,
        CancellationToken cancellationToken = default);
}
