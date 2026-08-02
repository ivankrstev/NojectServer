using MimeKit;

namespace NojectServer.Modules.Identity.Application.Email;

/// <summary>
/// Defines a transport capable of delivering fully composed email messages.
/// </summary>
/// <remarks>
/// This abstraction allows application email composition to remain independent
/// of SMTP or any other external delivery provider.
/// </remarks>
public interface IEmailSender
{
    /// <summary>
    /// Delivers an email message using the configured transport.
    /// </summary>
    /// <param name="message">The fully composed email message to deliver.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous delivery operation.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="message"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through
    /// <paramref name="cancellationToken"/>.
    /// </exception>
    Task SendAsync(
        MimeMessage message,
        CancellationToken cancellationToken = default);
}
