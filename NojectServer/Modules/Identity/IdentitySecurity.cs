namespace NojectServer.Modules.Identity;

/// <summary>
/// Names used by Identity authentication, authorization, and rate-limit policies.
/// </summary>
internal static class IdentitySecurity
{
    public const string PendingEmailVerificationAuthenticationScheme =
        "PendingEmailVerification";

    public const string PendingEmailVerificationPolicy =
        "Identity.PendingEmailVerification";

    public const string LoginRateLimitPolicy = "Identity.Login";

    public const string PendingVerificationRateLimitPolicy =
        "Identity.PendingVerificationActions";

    public const string VerificationResendRateLimitPolicy =
        "Identity.VerificationResend";
}
