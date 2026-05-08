using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;
using Microsoft.EntityFrameworkCore;

namespace CardCheesi.Game.Persistence;

public class GameRepository : IGameRepository
{
    private readonly AppDbContext _db;

    public GameRepository(AppDbContext db) => _db = db;

    public async Task SaveAsync(IGameState gameState, CancellationToken cancellationToken = default)
    {
        var concreteState = (DomainModels.GameState)gameState;
        var existing = await _db.Games.FindAsync([concreteState.Id], cancellationToken);

        if (existing is null)
        {
            await _db.Games.AddAsync(
                new GameEntity { Id = concreteState.Id, GameCode = concreteState.GameCode, State = concreteState },
                cancellationToken);
        }
        else
        {
            existing.State = concreteState;
            _db.Games.Update(existing);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IGameState?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Games.FindAsync([id], cancellationToken);
        return entity?.State;
    }

    public async Task<IGameState?> GetByCodeAsync(string gameCode, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Games.AsNoTracking()
            .FirstOrDefaultAsync(e => e.GameCode == gameCode, cancellationToken);
        return entity?.State;
    }
}
