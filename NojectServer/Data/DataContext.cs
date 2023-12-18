using Microsoft.EntityFrameworkCore;
using NojectServer.Models;

namespace NojectServer.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Collaborator> Collaborators { get; set; }
        public DbSet<Models.Task> Tasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
}