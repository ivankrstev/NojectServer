using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Application.Authentication.PendingEmailVerification;

/// <summary>
/// Provides the small set of account operations available while an email
/// address is still unverified.
/// </summary>
public interface IPendingEmailVerificationService
{
    /// <summary>
    /// Sends a replacement verification message for the current pending account.
    /// </summary>
    Task<Result> ResendAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the pending account's email and sends a verification message to it.
    /// </summary>
    Task<Result> ChangeEmailAsync(
        Guid userId,
        ChangePendingEmailInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes an unverified account after reconfirming its password.
    /// </summary>
    Task<Result> DeleteAccountAsync(
        Guid userId,
        DeletePendingAccountInput input,
        CancellationToken cancellationToken = default);
}
