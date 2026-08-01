namespace NojectServer.Modules.Identity.Application.Authentication.Login;

/// <summary>
/// Contains the information required to perform a login.
/// </summary>
/// <param name="Email">The account email address.</param>
/// <param name="Password">The account password.</param>
public sealed record LoginInput(
    string? Email,
    string? Password);
