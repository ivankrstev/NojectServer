using NojectServer.Utils.ResultPattern;

namespace NojectServer.Modules.Identity.Application.Authentication.Register;

/// <summary>
/// Contains standardized errors returned by user registration operations.
/// </summary>
internal static class RegisterErrors
{
    public static readonly ErrorDetails EmailAlreadyRegistered = new(
        "Register.EmailAlreadyRegistered",
        "An account with this email address already exists.",
        StatusCodes.Status409Conflict);
}
