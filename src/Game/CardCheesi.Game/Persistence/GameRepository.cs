using CardCheesi.Game.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CardCheesi.Game.Persistence;

public class GameRepository : IGameRepository
{
    private readonly AppDbContext _db;

    public GameRepository(AppDbContext db) => _db = db;

    public async Task SaveAsync(GameState gameState, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Games.FindAsync([gameState.Id], cancellationToken);

        if (existing is null)
        {
            await _db.Games.AddAsync(
                new GameEntity { Id = gameState.Id, GameCode = gameState.GameCode, State = gameState },
                cancellationToken);
        }
        else
        {
            existing.State = gameState;
            _db.Games.Update(existing);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<GameState?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Games.FindAsync([id], cancellationToken);
        return entity?.State;
    }

    public async Task<GameState?> GetByCodeAsync(string gameCode, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Games.AsNoTracking()
            .FirstOrDefaultAsync(e => e.GameCode == gameCode, cancellationToken);
        return entity?.State;
    }
}
