namespace NojectServer.Modules.Identity.Application.Authentication.Register;

public sealed record RegisterInput(
    string? Email,
    string? FullName,
    string? Password,
    string? ConfirmPassword);
