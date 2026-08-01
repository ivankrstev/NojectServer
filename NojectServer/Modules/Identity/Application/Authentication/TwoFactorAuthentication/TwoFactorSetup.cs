namespace NojectServer.Modules.Identity.Application.Authentication.TwoFactorAuthentication;

/// <summary>
/// Represents the result of generating a two-factor authentication setup code, including the manual key and provisioning URI.
/// </summary>
public sealed record TwoFactorSetup(
    string ManualKey,
    string ProvisioningUri);
