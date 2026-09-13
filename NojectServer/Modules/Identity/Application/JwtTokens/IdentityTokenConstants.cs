namespace NojectServer.Modules.Identity.Application.JwtTokens;

/// <summary>
/// Claim names and purpose values shared by Identity token producers and policies.
/// </summary>
internal static class IdentityTokenConstants
{
    public const string TokenPurposeClaimType = "token_use";
    public const string AccessTokenPurpose = "access";
    public const string TfaTokenPurpose = "tfa";
    public const string PendingEmailVerificationTokenPurpose =
        "email_verification_pending";
}
