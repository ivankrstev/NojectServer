namespace NojectServer.Modules.Identity.Application.Authentication.PasswordReset;

/// <summary>
/// Contains the information required to complete a password reset.
/// </summary>
/// <param name="ResetToken">The opaque token received through the reset link.</param>
/// <param name="NewPassword">The replacement password.</param>
/// <param name="ConfirmNewPassword">Confirmation of the replacement password.</param>
public sealed record ResetPasswordInput(
    string? ResetToken,
    string? NewPassword,
    string? ConfirmNewPassword);
