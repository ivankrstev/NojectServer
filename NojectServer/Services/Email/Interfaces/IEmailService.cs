using NojectServer.Models;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Services.Email.Interfaces;

/// <summary>
/// Service for sending various types of application emails to users
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email with a verification link to a user
    /// </summary>
    /// <param name="user">The user to send the verification email to</param>
    /// <returns>A task representing the asynchronous email sending operation</returns>
    Task SendVerificationLinkAsync(User user);

    /// <summary>
    /// Sends an email with a password reset link to a user
    /// </summary>
    /// <param name="user">The user to send the password reset email to</param>
    /// <returns>A task representing the asynchronous email sending operation</returns>
    Task SendResetPasswordLinkAsync(User user);
}
