using Microsoft.EntityFrameworkCore;
using TaskManager.Models;

namespace TaskManager.Data;

public class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskRemark> Remarks => Set<TaskRemark>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Description).HasMaxLength(2000);
            entity.Property(t => t.AssignedTo).HasMaxLength(100);
            entity.Property(t => t.AssignedBy).HasMaxLength(100);
            entity.Property(t => t.Status).HasConversion<string>();
            entity.HasMany(t => t.Remarks).WithOne().HasForeignKey(r => r.TaskId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskRemark>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Author).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Content).IsRequired().HasMaxLength(1000);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Recipient).IsRequired().HasMaxLength(100);
            entity.Property(n => n.Message).IsRequired().HasMaxLength(500);
            entity.Property(n => n.Type).HasConversion<string>();
        });
    }
}
