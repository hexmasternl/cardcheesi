namespace CardCheesi.Game.Abstractions;

public record Team(
    Guid Id,
    IReadOnlyList<Player> Players);
