using Microsoft.EntityFrameworkCore;
using NojectServer.Models;

namespace NojectServer.Data;

public class DataContext : DbContext
{
    public DataContext() { }

    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<Project> Projects { get; set; }
    public virtual DbSet<Collaborator> Collaborators { get; set; }
    public virtual DbSet<Models.Task> Tasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Models.Task>()
            .HasOne(t => t.NextTask)
            .WithMany()
            .HasForeignKey(t => new { t.Next, t.ProjectId });

        modelBuilder.Entity<Project>()
            .HasOne(p => p.Task)
            .WithMany()
            .HasForeignKey(p => new { p.FirstTask, p.Id });
    }
}