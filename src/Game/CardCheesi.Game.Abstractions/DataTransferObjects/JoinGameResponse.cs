namespace CardCheesi.Game.Abstractions.DataTransferObjects;

public sealed record JoinGameResponse(Guid GameId, Guid PlayerId, string GameCode);
