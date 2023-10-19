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

        public List<string> Validate()
        {
            List<string> errors = new() { };
            if (!Regex.IsMatch(Password, "(?=.*[a-z])"))
            {
                errors.Add("Password must include at least one lowercase letter");
            }
            if (!Regex.IsMatch(Password, "(?=.*[A-Z])"))
            {
                errors.Add("Password must include at least one uppercase letter");
            }
            if (!Regex.IsMatch(Password, "(?=.*\\d)"))
            {
                errors.Add("Password must include at least one digit");
            }
            if (!Password.Equals(ConfirmPassword))
            {
                errors.Add("Passwords must match");
            }
            return errors;
        }
    }
}