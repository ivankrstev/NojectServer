namespace NojectServer.Modules.Identity.Application.Authentication.Login;

/// <summary>
/// Contains the information required to complete a two-factor login.
/// </summary>
/// <param name="TfaToken"></param>
/// <param name="Code"></param>
public sealed record CompleteTwoFactorLoginInput(
    string? TfaToken,
    string? Code);
