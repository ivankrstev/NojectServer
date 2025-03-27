using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace NojectServer.Models.Requests.Auth;

/// <summary>
/// Represents a request to verify a user's email address.
/// Contains the email address to verify and the associated verification token.
/// </summary>
public class VerifyEmailRequest
{
    [FromQuery]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [FromQuery]
    [Required]
    public string Token { get; set; } = string.Empty;
}
