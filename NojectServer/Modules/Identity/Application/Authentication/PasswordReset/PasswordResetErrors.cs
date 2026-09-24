using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Application.Authentication.PasswordReset;

/// <summary>
/// Contains standardized errors returned by password-reset operations.
/// </summary>
internal static class PasswordResetErrors
{
    public static readonly ErrorDetails InvalidOrExpiredToken = new(
        "PasswordReset.InvalidOrExpiredToken",
        "The password reset token is invalid or has expired.",
        StatusCodes.Status400BadRequest);
}
