using CardCheesi.Game.DomainModels;

namespace CardCheesi.Game.Persistence;

public sealed class GameEntity
{
    public Guid Id { get; set; }
    public string GameCode { get; set; } = string.Empty;
    public GameState State { get; set; } = null!;
}
