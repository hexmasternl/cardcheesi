namespace CardCheesi.Players.Persistence;

public sealed class PlayerEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }

    public ICollection<RefreshTokenEntity> RefreshTokens { get; set; } = [];
}
