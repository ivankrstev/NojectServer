using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NojectServer.Models;

[Table("tasks")]
[PrimaryKey(nameof(Id), nameof(ProjectId))]
public class Task
{
    [Column("task_id")]
    [Required]
    public int Id { get; set; }

    [Column("level")]
    public int Level { get; set; } = 0;

    [Column("value")]
    public string Value { get; set; } = string.Empty;

    [Column("next")]
    public int? Next { get; set; }

    [JsonIgnore]
    public virtual Task? NextTask { get; set; }

    [Column("created_on")]
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    [Column("last_modified_on")]
    public DateTime? LastModifiedOn { get; set; }

    [Column("completed")]
    public bool Completed { get; set; }

    [Column("completed_by")]
    [ForeignKey("UserWhoCompleted")]
    public string? CompletedBy { get; set; }

    [JsonIgnore]
    public virtual User? UserWhoCompleted { get; set; }

    [Column("created_by")]
    [ForeignKey("UserWhoCreated")]
    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    [JsonIgnore]
    public virtual User? UserWhoCreated { get; set; }

    [Column("project_id")]
    [ForeignKey("Project")]
    [Required]
    [JsonIgnore]
    public Guid ProjectId { get; set; }

    [JsonIgnore]
    public virtual Project? Project { get; set; }
}