using System.ComponentModel.DataAnnotations;

namespace NojectServer.Models
{
    public class UserLoginRequest
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}