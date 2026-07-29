namespace NojectServer.Modules.Identity.Application.Authentication.EmailVerification;

/// <summary>
/// Contains the email address requesting a new verification link.
/// </summary>
/// <param name="Email">The account email address.</param>
public sealed record RequestEmailVerificationInput(string? Email);
