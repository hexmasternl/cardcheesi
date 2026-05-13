using CardCheesi.Auth;

namespace CardCheesi.Players.Api.Endpoints;

internal static class CookieHelper
{
    internal const string RefreshCookieName = "cc_refresh";

    internal static CookieOptions BuildRefreshCookieOptions(JwtSettings settings) => new()
    {
        HttpOnly = true,
        Secure = settings.CookieSecure,
        SameSite = SameSiteMode.Strict,
        Path = "/api/players/refresh",
        MaxAge = TimeSpan.FromSeconds(2592000),
    };
}
