namespace CardCheesi.Game.Abstractions.DataTransferObjects;

public sealed record CreateGameResponse(Guid GameId, string GameCode);
