using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Api.Auth;
using CardCheesi.Game.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CardCheesi.Game.Api.Features.RefreshToken;

public sealed class RefreshTokenHandler : ICommandHandler<RefreshTokenCommand, RefreshTokenResult?>
{
    private readonly AppDbContext _db;
    private readonly IOptions<JwtSettings> _jwtOptions;

    public RefreshTokenHandler(AppDbContext db, IOptions<JwtSettings> jwtOptions)
    {
        _db = db;
        _jwtOptions = jwtOptions;
    }

    public async Task<RefreshTokenResult?> Handle(RefreshTokenCommand command, CancellationToken ct)
    {
        var tokenHash = JwtTokenService.ComputeSha256Hex(command.RawCookieValue);
        var existingToken = await _db.RefreshTokens
            .Include(t => t.Player)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (existingToken is null)
            return null;

        if (existingToken.RevokedAt.HasValue)
        {
            // Theft detection: revoke all active tokens for this player
            var allActiveTokens = await _db.RefreshTokens
                .Where(t => t.PlayerId == existingToken.PlayerId && t.RevokedAt == null)
                .ToListAsync(ct);

            var revokeTime = DateTime.UtcNow;
            foreach (var token in allActiveTokens)
                token.RevokedAt = revokeTime;

            await _db.SaveChangesAsync(ct);
            return null;
        }

        if (existingToken.ExpiresAt < DateTime.UtcNow)
            return null;

        var settings = _jwtOptions.Value;
        var utcNow = DateTime.UtcNow;

        existingToken.RevokedAt = utcNow;
        existingToken.Player.LastSeenAt = utcNow;

        var (rawToken, hash) = JwtTokenService.GenerateRefreshToken();
        var newRefreshToken = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = existingToken.PlayerId,
            TokenHash = hash,
            CreatedAt = utcNow,
            ExpiresAt = utcNow.AddDays(settings.RefreshTokenExpiryDays),
        };

        _db.RefreshTokens.Add(newRefreshToken);
        await _db.SaveChangesAsync(ct);

        var accessToken = JwtTokenService.GenerateAccessToken(
            settings,
            existingToken.PlayerId,
            existingToken.Player.Name);

        return new RefreshTokenResult(accessToken, rawToken);
    }
}
