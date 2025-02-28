using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NojectServer.Models;

[Table("collaborators")]
[PrimaryKey(nameof(ProjectId), nameof(CollaboratorId))]
public class Collaborator
{
    [Column("project_id")]
    [ForeignKey("Project")]
    [Required]
    public Guid ProjectId { get; set; } = Guid.Empty;

    [JsonIgnore]
    public virtual Project? Project { get; set; }

    [Column("user_id")]
    [ForeignKey("User")]
    [Required]
    public string CollaboratorId { get; set; } = string.Empty;

    [JsonIgnore]
    public virtual User? User { get; set; }
}