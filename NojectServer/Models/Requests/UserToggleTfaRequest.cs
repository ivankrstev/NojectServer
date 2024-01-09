using System.ComponentModel.DataAnnotations;

namespace NojectServer.Models.Requests
{
    public class UserToggleTfaRequest
    {
        [Required(ErrorMessage = "The Two-Factor Code is required")]
        public string TwoFactorCode { get; set; } = string.Empty;
    }
}