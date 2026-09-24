namespace NojectServer.Modules.Identity.Application.Authentication.Register;

/// <summary>
/// Contains the information required to create a new user account.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="FullName">The user's full name.</param>
/// <param name="Password">The plain-text password to hash and store.</param>
/// <param name="ConfirmPassword">The confirmation of the plain-text password.</param>
public sealed record RegisterInput(
    string? Email,
    string? FullName,
    string? Password,
    string? ConfirmPassword);
