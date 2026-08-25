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

        modelBuilder.Entity<Result>()
            .HasIndex(r => r.FileName);

        modelBuilder.Entity<Value>()
            .HasIndex(v => new { v.ResultId, v.Date });
    }
}
