using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NojectServer.Models;

[Table("refresh_tokens")]
[PrimaryKey(nameof(UserId), nameof(Token))]
public class RefreshToken
{
    [Column("user_id")]
    [ForeignKey("User")]
    [Required]
    [StringLength(62)]
    public Guid UserId { get; set; } = Guid.Empty;

    public virtual User? User { get; set; }

    [Column("token")]
    [Required]
    public string Token { get; set; } = string.Empty;

    [Column("valid_until")]
    public DateTime ExpireDate { get; set; } = DateTime.UtcNow.AddDays(14);
}
