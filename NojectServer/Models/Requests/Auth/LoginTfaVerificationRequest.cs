using System.ComponentModel.DataAnnotations;

namespace NojectServer.Models.Requests.Auth;

/// <summary>
/// Represents a request to complete the two-factor authentication login process.
/// Contains the verification code provided by the user and the JWT token from the initial login attempt.
/// This request is used to verify the second authentication factor after successful email/password validation.
/// </summary>
public class LoginTfaVerificationRequest
{
    [Required(ErrorMessage = "The two factor code is required")]
    public string TwoFactorCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "The JWT token is required")]
    public string JwtToken { get; set; } = string.Empty;
}
