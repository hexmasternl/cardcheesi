using CardCheesi.Game.Abstractions;

namespace CardCheesi.Game.Persistence;

public class GameEntity
{
    public Guid Id { get; set; }
    public string GameCode { get; set; } = string.Empty;
    public GameState State { get; set; } = null!;
}
