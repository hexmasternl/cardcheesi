namespace CardCheesi.Game.Features.DisposeHand;

public sealed record DisposeHandCommand(string GameCode, Guid PlayerId);
public sealed record DisposeHandResult;
