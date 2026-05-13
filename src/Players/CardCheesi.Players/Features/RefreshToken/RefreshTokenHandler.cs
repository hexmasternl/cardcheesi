using CardCheesi.Auth;
using CardCheesi.Core;
using CardCheesi.Players.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CardCheesi.Players.Features.RefreshToken;

public sealed class RefreshTokenHandler : ICommandHandler<RefreshTokenCommand, RefreshTokenResult?>
{
    private readonly PlayersDbContext _db;
    private readonly IJwtTokenService _jwtService;
    private readonly IOptions<JwtSettings> _jwtOptions;

    public RefreshTokenHandler(PlayersDbContext db, IJwtTokenService jwtService, IOptions<JwtSettings> jwtOptions)
    {
        _db = db;
        _jwtService = jwtService;
        _jwtOptions = jwtOptions;
    }

    public async Task<RefreshTokenResult?> Handle(RefreshTokenCommand command, CancellationToken ct)
    {
        var tokenHash = _jwtService.ComputeSha256Hex(command.RawCookieValue);
        var existingToken = await _db.RefreshTokens
            .Include(t => t.Player)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (existingToken is null)
            return null;

        if (existingToken.RevokedAt.HasValue)
        {
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

        var (rawToken, hash) = _jwtService.GenerateRefreshToken();
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

        var accessToken = _jwtService.GenerateAccessToken(
            existingToken.PlayerId,
            existingToken.Player.Name);

        return new RefreshTokenResult(accessToken, rawToken);
    }
}
