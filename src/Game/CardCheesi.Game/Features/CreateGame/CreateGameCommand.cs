namespace CardCheesi.Game.Features.CreateGame;

public sealed record CreateGameCommand(string PlayerName, Guid PlayerId);

public sealed record CreateGameResult(Guid GameId, string GameCode);
