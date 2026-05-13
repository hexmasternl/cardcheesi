using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.Abstractions;

public interface IGameRepository
{
    Task SaveAsync(IGameState gameState, CancellationToken cancellationToken = default);
    Task<IGameState?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IGameState?> GetByCodeAsync(string gameCode, CancellationToken cancellationToken = default);
}
