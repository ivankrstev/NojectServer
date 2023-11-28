using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NojectServer.Models
{
    public class UserRegisterRequest
    {
        [Required, EmailAddress, MaxLength(62)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string FullName { get; set; } = string.Empty;

        [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string ConfirmPassword { get; set; } = string.Empty;

        public object? Validate()
        {
            if (!Regex.IsMatch(Password, "(?=.*[A-Z])") && !Regex.IsMatch(Password, "(?=.*[a-z])") && !Regex.IsMatch(Password, "(?=.*\\d)"))
                return (new { error = "Password Requirements Not Met", message = "Password must include at least one uppercase letter, one lowercase letter and one number" });
            else
            {
                if (!Regex.IsMatch(Password, "(?=.*[A-Z])") && !Regex.IsMatch(Password, "(?=.*[a-z])") && Regex.IsMatch(Password, "(?=.*\\d)"))
                    return (new { error = "Password Requirements Not Met", message = "Password must include at least one uppercase letter and one lowercase letter" });
                else if (!Regex.IsMatch(Password, "(?=.*[A-Z])") && Regex.IsMatch(Password, "(?=.*[a-z])") && !Regex.IsMatch(Password, "(?=.*\\d)"))
                    return (new { error = "Password Requirements Not Met", message = "Password must include at least one uppercase letter and one number" });
                else if (Regex.IsMatch(Password, "(?=.*[A-Z])") && !Regex.IsMatch(Password, "(?=.*[a-z])") && !Regex.IsMatch(Password, "(?=.*\\d)"))
                    return (new { error = "Password Requirements Not Met", message = "Password must include at least one lowercase letter and one number" });
                else if (!Regex.IsMatch(Password, "(?=.*[A-Z])"))
                    return (new { error = "Password Requirements Not Met", message = "Password must include at least one uppercase letter" });
                else if (!Regex.IsMatch(Password, "(?=.*[a-z])"))
                    return (new { error = "Password Requirements Not Met", message = "Password must include at least one lowercase letter" });
                else if (!Regex.IsMatch(Password, "(?=.*\\d)")) return (new { error = "Password Requirements Not Met", message = "Password must include at least one number" });
            }
            if (!Password.Equals(ConfirmPassword))
            {
                return (new { error = "Password Requirements Not Met", message = "Passwords must match" });
            }
            return null;
        }
    }
}