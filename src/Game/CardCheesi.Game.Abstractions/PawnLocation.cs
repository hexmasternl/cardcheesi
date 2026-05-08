namespace CardCheesi.Game.Abstractions;

public abstract record PawnLocation;

public record ReserveLocation : PawnLocation;

/// <param name="Position">1–64 on the shared board loop.</param>
public record BoardLocation(int Position) : PawnLocation;

/// <param name="Slot">1–4 in the player's personal finish track.</param>
public record FinishLocation(int Slot) : PawnLocation;
