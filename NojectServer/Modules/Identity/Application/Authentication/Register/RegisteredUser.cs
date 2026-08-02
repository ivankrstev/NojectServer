namespace NojectServer.Modules.Identity.Application.Authentication.Register;

/// <summary>
/// Information about a newly registered user account.
/// </summary>
public sealed record RegisteredUser(
    Guid UserId,
    string Email,
    string FullName);
