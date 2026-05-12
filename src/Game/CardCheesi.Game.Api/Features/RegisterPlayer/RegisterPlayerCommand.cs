namespace CardCheesi.Game.Api.Features.RegisterPlayer;

public sealed record RegisterPlayerCommand(string Name);

public sealed record RegisterPlayerResult(string AccessToken, string RawRefreshToken);
