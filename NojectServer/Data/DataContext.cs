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
            // Add only Task.Id as foreign key to the Next task, instead of both primary keys
            modelBuilder.Entity<Models.Task>()
                .HasOne(t => t.NextTask)
                .WithMany()
                .HasForeignKey(t => t.Next)
                .HasPrincipalKey(t => t.Id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Task)
                .WithMany()
                .HasForeignKey(p => p.FirstTask)
                .HasPrincipalKey(t => t.Id)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}