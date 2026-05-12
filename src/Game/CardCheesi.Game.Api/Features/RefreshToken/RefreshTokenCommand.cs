namespace CardCheesi.Game.Api.Features.RefreshToken;

public sealed record RefreshTokenCommand(string RawCookieValue);

public sealed record RefreshTokenResult(string AccessToken, string RawRefreshToken);
