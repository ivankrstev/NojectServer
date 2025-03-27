using System.ComponentModel.DataAnnotations;

namespace NojectServer.Models.Requests.Auth;

/// <summary>
/// Represents a request to authenticate a user in the system.
/// Contains the user credentials required for login.
/// </summary>
public class LoginRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
