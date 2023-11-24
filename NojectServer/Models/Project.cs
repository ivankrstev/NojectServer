using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NojectServer.Models
{
    [Table("projects")]
    public class Project
    {
        [Column("project_id")]
        [Key, Required]
        public Guid Id { get; set; } = new Guid();

        [Column("name")]
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Column("created_by")]
        [ForeignKey("User")]
        [Required]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonIgnore]
        public virtual User? User { get; set; }

        [Column("created_on")]
        [Required]
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        [Column("color", TypeName = "char(7)")]
        [Required]
        public string Color { get; set; } = string.Empty;

        [Column("background_color", TypeName = "char(7)")]
        [Required]
        public string BackgroundColor { get; set; } = string.Empty;
    }
}