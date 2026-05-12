namespace CardCheesi.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = "cardcheesi-api";
    public string Audience { get; init; } = "cardcheesi-api";
    public int AccessTokenExpiryMinutes { get; init; } = 10;
    public int RefreshTokenExpiryDays { get; init; } = 30;
    public bool CookieSecure { get; init; } = true;
}
