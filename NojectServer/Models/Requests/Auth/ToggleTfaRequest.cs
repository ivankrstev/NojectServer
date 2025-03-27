using System.ComponentModel.DataAnnotations;

namespace NojectServer.Models.Requests.Auth;

/// <summary>
/// Represents a request to enable or disable two-factor authentication for a user.
/// Contains the verification code required to confirm the two-factor authentication change.
/// </summary>
public class ToggleTfaRequest
{
    [Required(ErrorMessage = "The two factor code is required")]
    public string TwoFactorCode { get; set; } = string.Empty;
}
