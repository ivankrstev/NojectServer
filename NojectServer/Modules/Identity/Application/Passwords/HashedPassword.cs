namespace NojectServer.Modules.Identity.Application.Passwords;

/// <summary>
/// Contains the password credentials persisted by the user aggregate.
/// </summary>
public sealed record HashedPassword(byte[] Hash, byte[] Salt);
