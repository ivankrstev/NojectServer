using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NojectServer.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("user_id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("email")]
    [Required]
    [StringLength(62)]
    public string Email { get; set; } = string.Empty;

    [Column("full_name")]
    [Required]
    [StringLength(50)]
    public string FullName { get; set; } = string.Empty;

    [Column("password")]
    [Required]
    public byte[] Password { get; set; } = [];

    [Column("password_salt")]
    [Required]
    public byte[] PasswordSalt { get; set; } = [];

    [Column("verification_token", TypeName = "char(128)")]
    public string? VerificationToken { get; set; }

    [Column("verified_at")]
    public DateTime? VerifiedAt { get; set; }

    [Column("password_reset_token", TypeName = "char(128)")]
    public string? PasswordResetToken { get; set; }

    [Column("reset_token_expires")]
    public DateTime? ResetTokenExpires { get; set; }

    [Column("tfa_enabled")]
    public bool TwoFactorEnabled { get; set; } = false;

    [Column("tfa_secret_key", TypeName = "char(32)")]
    public string? TwoFactorSecretKey { get; set; }
}
