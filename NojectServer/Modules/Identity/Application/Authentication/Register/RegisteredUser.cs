namespace NojectServer.Modules.Identity.Application.Authentication.Register;

public sealed record RegisteredUser(
    Guid UserId,
    string Email,
    string FullName);
