using Bogus;
using CardCheesi.Auth;
using CardCheesi.Game.Persistence;

namespace CardCheesi.Players.Tests.Factories;

internal static class PlayerFactory
{
    private static readonly Faker<PlayerEntity> _faker = new Faker<PlayerEntity>()
        .RuleFor(p => p.Id, _ => Guid.NewGuid())
        .RuleFor(p => p.Name, f => f.Internet.UserName())
        .RuleFor(p => p.CreatedAt, f => f.Date.Past().ToUniversalTime())
        .RuleFor(p => p.LastSeenAt, (f, p) => f.Date.Between(p.CreatedAt, DateTime.UtcNow).ToUniversalTime());

    public static PlayerEntity Create(
        Guid? id = null,
        string? name = null,
        DateTime? createdAt = null,
        DateTime? lastSeenAt = null)
    {
        var entity = _faker.Generate();

        if (id.HasValue) entity.Id = id.Value;
        if (name is not null) entity.Name = name;
        if (createdAt.HasValue) entity.CreatedAt = createdAt.Value;
        if (lastSeenAt.HasValue) entity.LastSeenAt = lastSeenAt.Value;

        return entity;
    }
}

internal static class JwtSettingsFactory
{
    public static JwtSettings Create(
        string? signingKey = null,
        int accessTokenExpiryMinutes = 10,
        int refreshTokenExpiryDays = 30,
        bool cookieSecure = false) => new()
    {
        SigningKey = signingKey ?? Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenExpiryMinutes = accessTokenExpiryMinutes,
        RefreshTokenExpiryDays = refreshTokenExpiryDays,
        CookieSecure = cookieSecure,
    };
}
