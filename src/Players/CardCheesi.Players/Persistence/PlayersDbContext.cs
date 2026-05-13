using Microsoft.EntityFrameworkCore;

namespace CardCheesi.Players.Persistence;

public sealed class PlayersDbContext : DbContext
{
    public PlayersDbContext(DbContextOptions<PlayersDbContext> options) : base(options) { }

    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
