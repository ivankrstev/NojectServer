namespace NojectServer.Modules.Identity.Application.Authentication.PasswordReset;

/// <summary>
/// Contains the email address for a password-reset request.
/// </summary>
/// <param name="Email">The account email address.</param>
public sealed record RequestPasswordResetInput(string? Email);
