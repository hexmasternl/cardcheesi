using Bogus;
using CardCheesi.Auth;
using CardCheesi.Game.Persistence;

namespace CardCheesi.Players.Tests.Factories;

internal static class RefreshTokenFactory
{
    private static readonly Faker _faker = new();

    public static RefreshTokenEntity Create(
        Guid? id = null,
        Guid? playerId = null,
        string? tokenHash = null,
        DateTime? createdAt = null,
        DateTime? expiresAt = null,
        DateTime? revokedAt = null)
    {
        var now = DateTime.UtcNow;
        return new RefreshTokenEntity
        {
            Id = id ?? Guid.NewGuid(),
            PlayerId = playerId ?? Guid.NewGuid(),
            TokenHash = tokenHash ?? _faker.Random.AlphaNumeric(64),
            CreatedAt = createdAt ?? now.AddMinutes(-5),
            ExpiresAt = expiresAt ?? now.AddDays(30),
            RevokedAt = revokedAt,
        };
    }

    public static (string rawToken, RefreshTokenEntity entity) CreateWithRawToken(
        Guid? playerId = null,
        DateTime? expiresAt = null,
        DateTime? revokedAt = null)
    {
        var (raw, hash) = JwtTokenService.GenerateRefreshToken();
        var entity = Create(
            playerId: playerId,
            tokenHash: hash,
            expiresAt: expiresAt,
            revokedAt: revokedAt);
        return (raw, entity);
    }
}
