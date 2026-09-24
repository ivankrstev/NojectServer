using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Application.Authentication.PendingEmailVerification;

/// <summary>
/// Contains standardized pending-email-verification workflow errors.
/// </summary>
internal static class PendingEmailVerificationErrors
{
    public static readonly ErrorDetails WorkflowNotAvailable = new(
        "PendingEmailVerification.NotAvailable",
        "This account is not awaiting email verification.",
        StatusCodes.Status403Forbidden);

    public static readonly ErrorDetails EmailUnchanged = new(
        "PendingEmailVerification.EmailUnchanged",
        "The new email address must be different from the current address.",
        StatusCodes.Status400BadRequest);

    public static readonly ErrorDetails EmailAlreadyRegistered = new(
        "PendingEmailVerification.EmailAlreadyRegistered",
        "An account with this email address already exists.",
        StatusCodes.Status409Conflict);

    public static readonly ErrorDetails InvalidPassword = new(
        "PendingEmailVerification.InvalidPassword",
        "The password is incorrect.",
        StatusCodes.Status401Unauthorized);
}
