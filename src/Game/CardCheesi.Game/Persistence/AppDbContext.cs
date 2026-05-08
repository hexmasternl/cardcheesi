using System.Text.Json;
using CardCheesi.Game.DomainModels;
using Microsoft.EntityFrameworkCore;

namespace CardCheesi.Game.Persistence;

public class AppDbContext : DbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<GameEntity> Games => Set<GameEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.GameCode).HasMaxLength(6).IsRequired();
            entity.HasIndex(e => e.GameCode).IsUnique();
            entity.Property(e => e.State)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonOptions),
                    v => JsonSerializer.Deserialize<GameState>(v, JsonOptions)!)
                .HasColumnType("jsonb")
                .IsRequired();
        });
    }
}
