namespace NojectServer.Modules.Identity.Application.Authentication.PendingEmailVerification;

/// <summary>
/// Contains the password confirmation required to delete a pending account.
/// </summary>
public sealed record DeletePendingAccountInput(string? Password);
