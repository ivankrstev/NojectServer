namespace NojectServer.Modules.Identity.Application.Authentication.Login;

public sealed record LoginInput(
    string? Email,
    string? Password);
