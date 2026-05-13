using CardCheesi.Auth;
using CardCheesi.Core;
using CardCheesi.Players.Persistence;
using Microsoft.Extensions.Options;

namespace CardCheesi.Players.Features.RegisterPlayer;

public sealed class RegisterPlayerHandler : ICommandHandler<RegisterPlayerCommand, RegisterPlayerResult>
{
    private readonly PlayersDbContext _db;
    private readonly IJwtTokenService _jwtService;
    private readonly IOptions<JwtSettings> _jwtOptions;

    public RegisterPlayerHandler(PlayersDbContext db, IJwtTokenService jwtService, IOptions<JwtSettings> jwtOptions)
    {
        _db = db;
        _jwtService = jwtService;
        _jwtOptions = jwtOptions;
    }

    public async Task<RegisterPlayerResult> Handle(RegisterPlayerCommand command, CancellationToken ct)
    {
        var settings = _jwtOptions.Value;
        var now = DateTime.UtcNow;
        var playerId = Guid.NewGuid();

        var player = new PlayerEntity
        {
            Id = playerId,
            Name = command.Name,
            CreatedAt = now,
            LastSeenAt = now,
        };

        _db.Players.Add(player);

        var (rawToken, tokenHash) = _jwtService.GenerateRefreshToken();
        var refreshToken = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddDays(settings.RefreshTokenExpiryDays),
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);

        var accessToken = _jwtService.GenerateAccessToken(playerId, command.Name);

        return new RegisterPlayerResult(accessToken, rawToken);
    }
}
