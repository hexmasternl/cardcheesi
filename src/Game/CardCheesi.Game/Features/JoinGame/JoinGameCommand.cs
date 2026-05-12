namespace CardCheesi.Game.Features.JoinGame;

public sealed record JoinGameCommand(string GameCode, Guid PlayerId, string PlayerName);

public sealed record JoinGameResult(Guid GameId, Guid PlayerId, string GameCode);
