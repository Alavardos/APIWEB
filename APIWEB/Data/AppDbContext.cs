using APIWEB.Models;
using Microsoft.EntityFrameworkCore;

namespace APIWEB.Data;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext>options) : base(options) { }

    public DbSet<Result> Results { get; set; } 
    public DbSet<Value> Values { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.FileName)
            .IsRequired();

            entity.HasIndex(r => r.FileName);

            entity.HasMany(r => r.Values)
            .WithOne(v => v.Result)
            .HasForeignKey(v => v.ResultId)
            .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Value>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.HasIndex(v => new { v.ResultId, v.Date });
        });
    }
}
