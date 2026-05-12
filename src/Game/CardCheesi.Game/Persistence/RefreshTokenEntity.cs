namespace CardCheesi.Game.Persistence;

public sealed class RefreshTokenEntity
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public PlayerEntity Player { get; set; } = null!;
}
