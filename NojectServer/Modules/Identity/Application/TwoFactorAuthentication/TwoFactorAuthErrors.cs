using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Application.TwoFactorAuthentication;

/// <summary>
/// Contains standardized errors returned by two-factor authentication operations.
/// </summary>
public static class TwoFactorAuthErrors
{
    public static readonly ErrorDetails InvalidUserId = new(
        "TwoFactor.InvalidUserId",
        "The user ID is invalid.",
        StatusCodes.Status400BadRequest);

    public static readonly ErrorDetails UserNotFound = new(
        "TwoFactor.UserNotFound",
        "The user was not found.",
        StatusCodes.Status404NotFound);

    public static readonly ErrorDetails AlreadyEnabled = new(
        "TwoFactor.AlreadyEnabled",
        "Two-factor authentication is already enabled.",
        StatusCodes.Status409Conflict);

    public static readonly ErrorDetails NotEnabled = new(
        "TwoFactor.NotEnabled",
        "Two-factor authentication is not enabled.",
        StatusCodes.Status400BadRequest);

    public static readonly ErrorDetails NotConfigured = new(
        "TwoFactor.NotConfigured",
        "Two-factor authentication setup has not been generated.",
        StatusCodes.Status400BadRequest);

    public static readonly ErrorDetails InvalidCode = new(
        "TwoFactor.InvalidCode",
        "The security code is invalid or has already been used.",
        StatusCodes.Status401Unauthorized);

    public static readonly ErrorDetails ConcurrentSetup = new(
        "TwoFactor.ConcurrentSetup",
        "Two-factor authentication setup changed during the operation. Please try again.",
        StatusCodes.Status409Conflict);
}
