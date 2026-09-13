namespace NojectServer.Modules.Identity.Application.Authentication.PendingEmailVerification;

/// <summary>
/// Contains a replacement email for an unverified account.
/// </summary>
public sealed record ChangePendingEmailInput(string? NewEmail);
