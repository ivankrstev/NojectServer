using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NojectServer.Models
{
    [Table("refresh_tokens")]
    [PrimaryKey(nameof(Email), nameof(Token))]
    public class RefreshToken
    {
        [Column("user_id")]
        [Required]
        [StringLength(62)]
        public string Email { get; set; } = string.Empty;

        [Column("token")]
        [Required]
        public string Token { get; set; } = string.Empty;

        [Column("valid_until")]
        public DateTime ExpireDate { get; set; } = DateTime.UtcNow.AddDays(14);
    }
}