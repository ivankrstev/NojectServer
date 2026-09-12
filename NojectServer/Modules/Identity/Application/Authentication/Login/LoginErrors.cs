using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Application.Authentication.Login;

/// <summary>
/// Contains standardized errors returned by login operations.
/// </summary>
internal static class LoginErrors
{
    public static readonly ErrorDetails InvalidCredentials = new(
        "Login.InvalidCredentials",
        "The email address or password is incorrect.",
        StatusCodes.Status401Unauthorized);

    public static readonly ErrorDetails EmailNotVerified = new(
        "Login.EmailNotVerified",
        "The email address must be verified before signing in.",
        StatusCodes.Status403Forbidden);

    public static readonly ErrorDetails InvalidOrExpiredTwoFactorChallenge = new(
        "Login.InvalidOrExpiredTwoFactorChallenge",
        "The two-factor authentication challenge is invalid or has expired.",
        StatusCodes.Status401Unauthorized);

    public static readonly ErrorDetails InvalidTwoFactorCode = new(
        "Login.InvalidTwoFactorCode",
        "The two-factor authentication code is invalid or has already been used.",
        StatusCodes.Status401Unauthorized);

    public static readonly ErrorDetails TwoFactorAuthenticationFailed = new(
        "Login.TwoFactorAuthenticationFailed",
        "Two-factor authentication could not be completed.",
        StatusCodes.Status401Unauthorized);

    public static readonly ErrorDetails AuthenticationCompletionFailed = new(
        "Login.AuthenticationCompletionFailed",
        "Authentication could not be completed. Please try again.",
        StatusCodes.Status500InternalServerError);
}
