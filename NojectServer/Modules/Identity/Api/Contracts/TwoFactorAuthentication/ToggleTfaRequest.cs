using System.ComponentModel.DataAnnotations;

namespace NojectServer.Modules.Identity.Api.Contracts.TwoFactorAuthentication;

/// <summary>
/// Represents a request to enable or disable two-factor authentication for a user.
/// Contains the verification code required to confirm the two-factor authentication change.
/// </summary>
public sealed class ToggleTfaRequest
{
    [Required(ErrorMessage = "The two factor code is required")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "The two factor code must be a 6-digit number")]
    public string Code { get; set; } = string.Empty;
}
