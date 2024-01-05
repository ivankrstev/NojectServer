using System.ComponentModel.DataAnnotations;

namespace NojectServer.Models.Requests
{
    public class UserVerifyTfaRequest
    {
        [Required(ErrorMessage = "The Two-Factor Code is required")]
        public string TwoFactorCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "The JWT Token is required")]
        public string JwtToken { get; set; } = string.Empty;
    }
}