namespace CardCheesi.Game.Abstractions;

public record GameState(
    Guid Id,
    IReadOnlyList<Team> Teams,
    IReadOnlyList<Player> Players,
    TurnState Turn,
    Deck Deck,
    IReadOnlyList<PlayerHand> Hands);
