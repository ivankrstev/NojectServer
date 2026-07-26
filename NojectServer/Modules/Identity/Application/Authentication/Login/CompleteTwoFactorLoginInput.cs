namespace NojectServer.Modules.Identity.Application.Authentication.Login;

public sealed record CompleteTwoFactorLoginInput(
    string? TfaToken,
    string? Code);
