using System.Text.Json;
using CardCheesi.Game.DomainModels;
using Microsoft.EntityFrameworkCore;

namespace CardCheesi.Game.Persistence;

public class AppDbContext : DbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<GameEntity> Games => Set<GameEntity>();
    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

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

        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastSeenAt).IsRequired();
        });

        modelBuilder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.HasOne(e => e.Player)
                .WithMany(p => p.RefreshTokens)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
