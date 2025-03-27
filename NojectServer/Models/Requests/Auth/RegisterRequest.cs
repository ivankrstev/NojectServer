using System.ComponentModel.DataAnnotations;
using static System.Text.RegularExpressions.Regex;

namespace NojectServer.Models.Requests.Auth;

/// <summary>
/// Represents a request to register a new user in the system.
/// Contains user registration information including email, full name, and password with comprehensive validation.
/// Provides custom password strength validation to ensure security requirements are met.
/// </summary>
public class RegisterRequest
{
    [Required]
    [MaxLength(62)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;

    public ValidationError? Validate()
    {
        if (!IsMatch(Password, "(?=.*[A-Z])") && !IsMatch(Password, "(?=.*[a-z])") &&
            !IsMatch(Password, "(?=.*\\d)"))
        {
            return new ValidationError("Password Requirements Not Met",
                "Password must include at least one uppercase letter, one lowercase letter and one number");
        }

        if (!IsMatch(Password, "(?=.*[A-Z])") && !IsMatch(Password, "(?=.*[a-z])") &&
            IsMatch(Password, "(?=.*\\d)"))
        {
            return new ValidationError("Password Requirements Not Met",
                "Password must include at least one uppercase letter and one lowercase letter");
        }

        if (!IsMatch(Password, "(?=.*[A-Z])") && IsMatch(Password, "(?=.*[a-z])") &&
            !IsMatch(Password, "(?=.*\\d)"))
        {
            return new ValidationError("Password Requirements Not Met",
                "Password must include at least one uppercase letter and one number");
        }

        if (IsMatch(Password, "(?=.*[A-Z])") && !IsMatch(Password, "(?=.*[a-z])") &&
            !IsMatch(Password, "(?=.*\\d)"))
        {
            return new ValidationError("Password Requirements Not Met",
                "Password must include at least one lowercase letter and one number");
        }

        if (!IsMatch(Password, "(?=.*[A-Z])"))
        {
            return new ValidationError("Password Requirements Not Met",
                "Password must include at least one uppercase letter");
        }

        if (!IsMatch(Password, "(?=.*[a-z])"))
        {
            return new ValidationError("Password Requirements Not Met",
                "Password must include at least one lowercase letter");
        }

        if (!IsMatch(Password, "(?=.*\\d)"))
        {
            return new ValidationError("Password Requirements Not Met",
                "Password must include at least one number");
        }

        return !Password.Equals(ConfirmPassword)
            ? new ValidationError("Password Requirements Not Met", "Passwords must match")
            : null;
    }
}

public class ValidationError(string error, string message)
{
    public string Error { get; } = error;
    public string Message { get; } = message;
}
